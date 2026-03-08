# よくある問題と解決方法

## 概要
本番環境でよく発生する問題とその解決方法をまとめています。

## アプリケーション起動・デプロイ

### 問題1: ECSタスクが起動しない

#### 症状
- タスクが immediately stopped
- runningCount が 0 のまま
- ヘルスチェックに到達しない

#### 確認方法
```bash
# タスク停止理由を確認
aws ecs describe-tasks \
  --cluster app-cluster \
  --tasks <task-arn> \
  --region ap-northeast-1 \
  --query 'tasks[0].[stoppedReason,containers[0].exitCode,containers[0].reason]'

# ECSイベントログ確認
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].events[0:5]' \
  --output table

# CloudWatch Logsを確認
aws logs tail /ecs/dotnet-app --since 10m --region ap-northeast-1
```

#### よくある原因と対処法

##### 原因1: イメージが見つからない
```
Error: CannotPullContainerError
```

**対処法**:
```bash
# ECRにイメージが存在するか確認
aws ecr describe-images \
  --repository-name dotnet-app \
  --region ap-northeast-1

# タスク定義のイメージURIが正しいか確認
aws ecs describe-task-definition \
  --task-definition dotnet-task \
  --region ap-northeast-1 \
  --query 'taskDefinition.containerDefinitions[0].image'
```

##### 原因2: メモリ不足
```
Error: OutOfMemoryError or exit code 137
```

**対処法**:
```bash
# タスク定義のメモリを増やす
{
  "memory": "1024"  // 512 → 1024 に増加
}

# 新しいタスク定義を登録してデプロイ
aws ecs register-task-definition --cli-input-json file://task-definition.json
aws ecs update-service --cluster app-cluster --service dotnet-service --task-definition dotnet-task:new-revision
```

##### 原因3: Secrets Manager へのアクセス失敗
```
Error: Unable to fetch secrets from AWS Secrets Manager
```

**対処法**:
```bash
# タスク実行ロールの権限を確認
aws iam get-role-policy \
  --role-name ecsTaskExecutionRole \
  --policy-name SecretsManagerPolicy

# 必要な権限:
{
  "Effect": "Allow",
  "Action": [
    "secretsmanager:GetSecretValue"
  ],
  "Resource": "arn:aws:secretsmanager:ap-northeast-1:*:secret:dotnet-app-secrets-*"
}
```

##### 原因4: アプリケーションの起動エラー
```
Error: Application startup exception
```

**対処法**:
```bash
# アプリケーションログを詳細に確認
aws logs tail /ecs/dotnet-app --since 10m --region ap-northeast-1 --format short

# ローカルで同じイメージを起動してテスト
docker run -p 8080:8080 110221759530.dkr.ecr.ap-northeast-1.amazonaws.com/dotnet-app:latest
```

---

### 問題2: デプロイが完了しない

#### 症状
- デプロイが "IN_PROGRESS" のまま進まない
- 新しいタスクが起動と停止を繰り返す
- タイムアウトでロールバックされる

#### 確認方法
```bash
# デプロイ状況を確認
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].deployments'
```

#### よくある原因と対処法

##### 原因1: ヘルスチェックが失敗
```
Health check failed
```

**対処法**:
```bash
# ヘルスチェックエンドポイントを手動で確認
# タスクのプライベートIPを取得
TASK_IP=$(aws ecs describe-tasks --cluster app-cluster --tasks <task-arn> --region ap-northeast-1 --query 'tasks[0].attachments[0].details[?name==`privateIPv4Address`].value' --output text)

# 直接アクセス（VPC内から）
curl http://$TASK_IP:8080/healthz

# ヘルスチェックエンドポイントが実装されているか確認
# アプリケーションコードをチェック
```

##### 原因2: ポートマッピングの問題
```
Target is not responding
```

**対処法**:
```bash
# タスク定義のポート設定を確認
aws ecs describe-task-definition \
  --task-definition dotnet-task \
  --region ap-northeast-1 \
  --query 'taskDefinition.containerDefinitions[0].portMappings'

# 期待される設定:
[
  {
    "containerPort": 8080,
    "protocol": "tcp"
  }
]

# アプリケーションが正しいポートでリッスンしているか確認
# Program.cs で設定を確認
```

---

## データベース接続

### 問題3: データベース接続エラー

#### 症状
- "Connection timeout"
- "Unable to connect to database"
- HTTP 500エラーが頻発

#### 確認方法
```bash
# アプリケーションログでエラー確認
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "database" \
  --start-time $(date -d '10 minutes ago' +%s)000 \
  --region ap-northeast-1

# Supabase接続テスト
psql "postgresql://postgres:[PASSWORD]@db.[PROJECT-REF].supabase.co:5432/postgres" -c "SELECT 1"
```

#### よくある原因と対処法

##### 原因1: 接続文字列が間違っている
**対処法**:
```bash
# Secrets Managerの値を確認
aws secretsmanager get-secret-value \
  --secret-id dotnet-app-secrets \
  --region ap-northeast-1 \
  --query 'SecretString' \
  --output text

# Supabase Dashboardで正しい接続情報を確認
# Settings → Database → Connection string
```

##### 原因2: 接続数が上限に達している
**対処法**:
```csharp
// Program.cs で接続プーリングを設定
services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MaxBatchSize(100);
    });
});

// 接続文字列に pooling parameters を追加
"Host=...;Database=...;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100"
```

##### 原因3: ネットワーク接続の問題
**対処法**:
```bash
# セキュリティグループの確認
aws ec2 describe-security-groups \
  --group-ids <security-group-id> \
  --region ap-northeast-1

# アウトバウンドルールで 5432 ポートが許可されているか確認

# DNS解決の確認
nslookup db.[PROJECT-REF].supabase.co
```

---

## パフォーマンス

### 問題4: レスポンスが遅い

#### 症状
- APIレスポンスタイムが2秒以上
- タイムアウトエラーが発生
- ユーザーから遅いとの報告

#### 確認方法
```bash
# ALBメトリクスでレスポンスタイムを確認
aws cloudwatch get-metric-statistics \
  --namespace AWS/ApplicationELB \
  --metric-name TargetResponseTime \
  --dimensions Name=LoadBalancer,Value=<load-balancer-full-name> \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average,Maximum \
  --region ap-northeast-1

# アプリケーションログで遅いリクエストを確認
aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "{ $.duration >= 2000 }" \
  --region ap-northeast-1
```

#### よくある原因と対処法

##### 原因1: データベースクエリが遅い
**対処法**:
```bash
# Supabase Dashboardでスロークエリを確認
# Database → Query Performance

# N+1問題を確認
# Entity Framework で Eager Loading を使用
context.Users.Include(u => u.Orders).ToList();

# インデックスを追加
CREATE INDEX idx_users_email ON users(email);
```

##### 原因2: CPU/メモリ不足
**対処法**:
```bash
# CPU使用率を確認
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average \
  --region ap-northeast-1

# リソースを増やす
# タスク定義で cpu と memory を増やす
{
  "cpu": "512",     // 256 → 512
  "memory": "1024"  // 512 → 1024
}
```

##### 原因3: 外部APIの遅延
**対処法**:
```csharp
// タイムアウトを設定
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(5)
};

// 非同期処理に変更
await externalService.CallApiAsync();

// キャッシュを導入
services.AddMemoryCache();
```

---

## ログ・監視

### 問題5: ログが出力されない

#### 症状
- CloudWatch Logsにログが表示されない
- デバッグ情報が見られない

#### 確認方法
```bash
# ロググループの存在確認
aws logs describe-log-groups \
  --log-group-name-prefix /ecs/dotnet-app \
  --region ap-northeast-1

# ログストリームの確認
aws logs describe-log-streams \
  --log-group-name /ecs/dotnet-app \
  --region ap-northeast-1 \
  --order-by LastEventTime \
  --descending \
  --max-items 5
```

#### よくある原因と対処法

##### 原因1: タスク定義のログ設定が間違っている
**対処法**:
```bash
# タスク定義のログ設定を確認
aws ecs describe-task-definition \
  --task-definition dotnet-task \
  --region ap-northeast-1 \
  --query 'taskDefinition.containerDefinitions[0].logConfiguration'

# 正しい設定:
{
  "logDriver": "awslogs",
  "options": {
    "awslogs-group": "/ecs/dotnet-app",
    "awslogs-region": "ap-northeast-1",
    "awslogs-stream-prefix": "ecs"
  }
}
```

##### 原因2: IAMロールに権限がない
**対処法**:
```bash
# タスク実行ロールに CloudWatch Logs 権限を追加
aws iam put-role-policy \
  --role-name ecsTaskExecutionRole \
  --policy-name CloudWatchLogsPolicy \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents"
      ],
      "Resource": "*"
    }]
  }'
```

##### 原因3: アプリケーションがログを出力していない
**対処法**:
```csharp
// Program.cs でコンソールログを有効化
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// コントローラーでロギング
_logger.LogInformation("Request received: {Path}", Request.Path);
```

---

## セキュリティ

### 問題6: Secrets Manager の値が取得できない

#### 症状
- 環境変数が空
- "Secret not found" エラー
- アプリケーションが設定不足で起動失敗

#### 確認方法
```bash
# Secretsが存在するか確認
aws secretsmanager describe-secret \
  --secret-id dotnet-app-secrets \
  --region ap-northeast-1

# Secret の値を確認
aws secretsmanager get-secret-value \
  --secret-id dotnet-app-secrets \
  --region ap-northeast-1 \
  --query 'SecretString' \
  --output text
```

#### よくある原因と対処法

##### 原因1: タスク定義でSecretsが正しく設定されていない
**対処法**:
```json
{
  "secrets": [
    {
      "name": "GEMINI_API_KEY",
      "valueFrom": "arn:aws:secretsmanager:ap-northeast-1:110221759530:secret:dotnet-app-secrets:GEMINI_API_KEY::"
    },
    {
      "name": "SUPABASE_URL",
      "valueFrom": "arn:aws:secretsmanager:ap-northeast-1:110221759530:secret:dotnet-app-secrets:SUPABASE_URL::"
    }
  ]
}
```

##### 原因2: IAMロールに権限がない
**対処法**:
```bash
# タスク実行ロールに Secrets Manager 権限を追加
aws iam put-role-policy \
  --role-name ecsTaskExecutionRole \
  --policy-name SecretsManagerPolicy \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue"
      ],
      "Resource": "arn:aws:secretsmanager:ap-northeast-1:*:secret:dotnet-app-secrets-*"
    }]
  }'
```

---

## ネットワーク

### 問題7: インターネットからアクセスできない

#### 症状
- ALBのDNS名にアクセスできない
- "Connection timed out"
- カスタムドメインが解決しない

#### 確認方法
```bash
# ALBの状態確認
aws elbv2 describe-load-balancers \
  --region ap-northeast-1 \
  --query 'LoadBalancers[*].[LoadBalancerName,DNSName,State]'

# ターゲットグループのヘルス確認
aws elbv2 describe-target-health \
  --target-group-arn <target-group-arn> \
  --region ap-northeast-1

# DNS解決確認
nslookup rya234.com
```

#### よくある原因と対処法

##### 原因1: セキュリティグループでポートが許可されていない
**対処法**:
```bash
# ALBのセキュリティグループ確認
aws ec2 describe-security-groups \
  --group-ids <alb-security-group-id> \
  --region ap-northeast-1

# インバウンドルールで 80, 443 ポートを許可
aws ec2 authorize-security-group-ingress \
  --group-id <alb-security-group-id> \
  --protocol tcp \
  --port 80 \
  --cidr 0.0.0.0/0 \
  --region ap-northeast-1
```

##### 原因2: ターゲットグループにターゲットが登録されていない
**対処法**:
```bash
# ターゲット登録状況を確認
aws elbv2 describe-target-health \
  --target-group-arn <target-group-arn> \
  --region ap-northeast-1

# ECSサービスがターゲットグループに正しく設定されているか確認
aws ecs describe-services \
  --cluster app-cluster \
  --services dotnet-service \
  --region ap-northeast-1 \
  --query 'services[0].loadBalancers'
```

---

## トラブルシューティングフローチャート

```
問題発生
    ↓
ログを確認
    ├─ エラーあり → エラーメッセージで検索
    └─ エラーなし → メトリクスを確認
             ├─ CPU高 → リソース不足
             ├─ Memory高 → メモリリーク or リソース不足
             └─ 正常 → ネットワークを確認
                     ├─ 接続失敗 → セキュリティグループ確認
                     └─ 接続成功 → アプリケーションロジック確認
```

---

## 関連ドキュメント

- [ログ解析ガイド](log-analysis.md)
- [パフォーマンスチューニング](performance-tuning.md)
- [監視概要](../monitoring/monitoring-overview.md)
- [デプロイトラブルシューティング](../deployment/automated-deployment.md#トラブルシューティング)

---

**最終更新日**: 2025-12-17
