# コスト最適化

## 概要
AWSコストを最適化し、効率的に運用する方法を説明します。

## 現状コストの把握

### AWS Cost Explorer での確認

```bash
# 過去30日間のコスト
aws ce get-cost-and-usage \
  --time-period Start=$(date -d '30 days ago' +%Y-%m-%d),End=$(date +%Y-%m-%d) \
  --granularity DAILY \
  --metrics BlendedCost \
  --region us-east-1

# サービス別コスト
aws ce get-cost-and-usage \
  --time-period Start=$(date -d '30 days ago' +%Y-%m-%d),End=$(date +%Y-%m-%d) \
  --granularity MONTHLY \
  --metrics BlendedCost \
  --group-by Type=DIMENSION,Key=SERVICE \
  --region us-east-1
```

### 主要コスト要素

| サービス | 主な課金項目 | 最適化のポイント |
|---------|------------|----------------|
| ECS Fargate | vCPU時間、メモリ時間 | 適切なリソース設定 |
| ALB | 時間、LCU | 不要なALBの削除 |
| ECR | ストレージ、データ転送 | 古いイメージの削除 |
| CloudWatch | ログ保存、メトリクス | 保持期間の設定 |
| NAT Gateway | 時間、データ転送 | 使用量の削減 |

---

## ECS Fargate の最適化

### 適切なリソース設定

#### 現在の使用率を確認

```bash
# CPU使用率（過去7日間の平均）
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '7 days ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 604800 \
  --statistics Average,Maximum \
  --region ap-northeast-1

# メモリ使用率（過去7日間の平均）
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name MemoryUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '7 days ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 604800 \
  --statistics Average,Maximum \
  --region ap-northeast-1
```

#### リソースの最適化

使用率が常に30%以下の場合、リソースを削減：

```json
// Before
{
  "cpu": "512",     // 0.5 vCPU
  "memory": "1024"  // 1 GB
}

// After (使用率が低い場合)
{
  "cpu": "256",     // 0.25 vCPU
  "memory": "512"   // 0.5 GB
}
```

### Fargate Spot の活用

開発・ステージング環境では Fargate Spot を使用してコスト削減（最大70%削減）：

```json
{
  "capacityProviderStrategy": [
    {
      "capacityProvider": "FARGATE_SPOT",
      "weight": 4,
      "base": 0
    },
    {
      "capacityProvider": "FARGATE",
      "weight": 1
    }
  ]
}
```

**注意**: 本番環境では FARGATE を推奨（可用性重視）

---

## CloudWatch の最適化

### ログ保持期間の設定

```bash
# 保持期間を7日に設定（デフォルトは無期限）
aws logs put-retention-policy \
  --log-group-name /ecs/dotnet-app \
  --retention-in-days 7 \
  --region ap-northeast-1

# 環境別の推奨保持期間
# 本番環境: 7-30日
# 開発環境: 1-3日
```

### 不要なロググループの削除

```bash
# 古いロググループを一覧表示
aws logs describe-log-groups \
  --region ap-northeast-1 \
  --query 'logGroups[?creationTime<`'$(date -d '90 days ago' +%s)'000`].[logGroupName,creationTime]' \
  --output table

# 不要なロググループを削除
aws logs delete-log-group \
  --log-group-name /ecs/old-app \
  --region ap-northeast-1
```

### Container Insights の無効化

開発環境では Container Insights を無効化してコスト削減：

```bash
# Container Insights を無効化
aws ecs update-cluster-settings \
  --cluster dev-cluster \
  --settings name=containerInsights,value=disabled \
  --region ap-northeast-1

# 関連ロググループを削除
aws logs delete-log-group \
  --log-group-name /aws/ecs/containerinsights/dev-cluster/performance \
  --region ap-northeast-1
```

---

## ECR の最適化

### 古いイメージの削除

```bash
# 最新10個以外のイメージを削除するライフサイクルポリシー
cat > ecr-lifecycle-policy.json <<EOF
{
  "rules": [
    {
      "rulePriority": 1,
      "description": "Keep last 10 images",
      "selection": {
        "tagStatus": "any",
        "countType": "imageCountMoreThan",
        "countNumber": 10
      },
      "action": {
        "type": "expire"
      }
    }
  ]
}
EOF

# ポリシーを適用
aws ecr put-lifecycle-policy \
  --repository-name dotnet-app \
  --lifecycle-policy-text file://ecr-lifecycle-policy.json \
  --region ap-northeast-1
```

### イメージサイズの削減

```dockerfile
# マルチステージビルドで最適化
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

# ランタイムのみを含む軽量イメージ
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "DotNetApp.dll"]
```

---

## データ転送の最適化

### NAT Gateway の削減

#### VPC Endpoint の使用

S3、ECR、Secrets Manager へのアクセスに VPC Endpoint を使用：

```bash
# S3 VPC Endpoint の作成
aws ec2 create-vpc-endpoint \
  --vpc-id vpc-xxx \
  --service-name com.amazonaws.ap-northeast-1.s3 \
  --route-table-ids rtb-xxx \
  --region ap-northeast-1

# ECR VPC Endpoint の作成（Interface型）
aws ec2 create-vpc-endpoint \
  --vpc-id vpc-xxx \
  --vpc-endpoint-type Interface \
  --service-name com.amazonaws.ap-northeast-1.ecr.api \
  --subnet-ids subnet-xxx \
  --security-group-ids sg-xxx \
  --region ap-northeast-1
```

**コスト削減効果**: NAT Gateway のデータ転送料金（$0.045/GB）を削減

---

## RDS / Supabase の最適化

### データベースの停止（開発環境）

```bash
# 使用していない時間帯はRDSを停止（最大7日間）
aws rds stop-db-instance \
  --db-instance-identifier dev-database \
  --region ap-northeast-1

# 翌朝に自動起動
aws rds start-db-instance \
  --db-instance-identifier dev-database \
  --region ap-northeast-1
```

### Supabase プランの見直し

| プラン | 月額 | 用途 |
|-------|------|------|
| Free | $0 | 開発・テスト |
| Pro | $25 | 小規模本番環境 |
| Team | $599 | 中規模本番環境 |

**最適化のポイント**:
- 開発環境は Free プラン
- 本番環境は Pro プランから開始
- 必要に応じてスケールアップ

---

## ALB の最適化

### 不要なリスナーの削除

```bash
# リスナー一覧
aws elbv2 describe-listeners \
  --load-balancer-arn <alb-arn> \
  --region ap-northeast-1

# 不要なリスナーを削除
aws elbv2 delete-listener \
  --listener-arn <listener-arn> \
  --region ap-northeast-1
```

### ALB の統合

複数の ALB がある場合、可能であれば統合してコスト削減。

---

## Auto Scaling によるコスト最適化

### スケジュールベースのスケーリング

```bash
# 夜間（0:00-6:00）はタスク数を0に
aws application-autoscaling put-scheduled-action \
  --service-namespace ecs \
  --resource-id service/dev-cluster/dev-service \
  --scalable-dimension ecs:service:DesiredCount \
  --scheduled-action-name scale-down-night \
  --schedule "cron(0 0 * * ? *)" \
  --scalable-target-action MinCapacity=0,MaxCapacity=0 \
  --region ap-northeast-1

# 朝（6:00）にタスク数を1に
aws application-autoscaling put-scheduled-action \
  --service-namespace ecs \
  --resource-id service/dev-cluster/dev-service \
  --scalable-dimension ecs:service:DesiredCount \
  --scheduled-action-name scale-up-morning \
  --schedule "cron(0 6 * * ? *)" \
  --scalable-target-action MinCapacity=1,MaxCapacity=2 \
  --region ap-northeast-1
```

---

## コスト分析とレポート

### 月次コストレポート

```bash
#!/bin/bash
# monthly-cost-report.sh

MONTH=$(date -d 'last month' +%Y-%m)

echo "=== Monthly Cost Report: $MONTH ==="

# 月次合計コスト
TOTAL_COST=$(aws ce get-cost-and-usage \
  --time-period Start=${MONTH}-01,End=$(date +%Y-%m)-01 \
  --granularity MONTHLY \
  --metrics BlendedCost \
  --region us-east-1 \
  --query 'ResultsByTime[0].Total.BlendedCost.Amount' \
  --output text)

echo "Total Cost: \$$TOTAL_COST"

# サービス別コスト
echo ""
echo "Cost by Service:"
aws ce get-cost-and-usage \
  --time-period Start=${MONTH}-01,End=$(date +%Y-%m)-01 \
  --granularity MONTHLY \
  --metrics BlendedCost \
  --group-by Type=DIMENSION,Key=SERVICE \
  --region us-east-1 \
  --query 'ResultsByTime[0].Groups[].[Keys[0],Metrics.BlendedCost.Amount]' \
  --output table | sort -k2 -rn | head -10

# 前月比
LAST_MONTH=$(date -d '2 months ago' +%Y-%m)
LAST_MONTH_COST=$(aws ce get-cost-and-usage \
  --time-period Start=${LAST_MONTH}-01,End=${MONTH}-01 \
  --granularity MONTHLY \
  --metrics BlendedCost \
  --region us-east-1 \
  --query 'ResultsByTime[0].Total.BlendedCost.Amount' \
  --output text)

DIFF=$(echo "$TOTAL_COST - $LAST_MONTH_COST" | bc)
PERCENT=$(echo "scale=2; ($DIFF / $LAST_MONTH_COST) * 100" | bc)

echo ""
echo "Comparison with last month:"
echo "Last month: \$$LAST_MONTH_COST"
echo "This month: \$$TOTAL_COST"
echo "Difference: \$$DIFF ($PERCENT%)"

echo ""
echo "=== Report Complete ==="
```

---

## コスト予算とアラート

### 予算アラートの設定

```bash
# 月額予算を設定
aws budgets create-budget \
  --account-id <account-id> \
  --budget file://budget.json \
  --notifications-with-subscribers file://notifications.json

# budget.json
{
  "BudgetName": "MonthlyBudget",
  "BudgetLimit": {
    "Amount": "50",
    "Unit": "USD"
  },
  "TimeUnit": "MONTHLY",
  "BudgetType": "COST"
}

# notifications.json
{
  "Notification": {
    "NotificationType": "ACTUAL",
    "ComparisonOperator": "GREATER_THAN",
    "Threshold": 80
  },
  "Subscribers": [
    {
      "SubscriptionType": "EMAIL",
      "Address": "your-email@example.com"
    }
  ]
}
```

---

## コスト最適化チェックリスト

### 月次チェック

```markdown
## コスト最適化チェックリスト

### ECS / Fargate
- [ ] タスクのCPU/メモリ使用率を確認
- [ ] 未使用タスク定義の削除
- [ ] 開発環境でFargate Spotの検討

### CloudWatch
- [ ] ログ保持期間の確認
- [ ] 不要なロググループの削除
- [ ] Container Insightsの必要性確認

### ECR
- [ ] 古いイメージの削除
- [ ] ライフサイクルポリシーの確認

### ネットワーク
- [ ] NAT Gateway の使用量確認
- [ ] VPC Endpoint の活用検討
- [ ] データ転送量の確認

### データベース
- [ ] 開発環境の停止スケジュール確認
- [ ] Supabase プランの見直し

### その他
- [ ] 未使用リソースの削除
- [ ] リザーブドインスタンスの検討
- [ ] Savings Plans の検討
```

---

## 予想される月額コスト（参考）

### 小規模構成（開発環境）

| サービス | 月額 |
|---------|------|
| ECS Fargate（0.25 vCPU, 0.5GB, 24h） | $10 |
| ALB | $20 |
| CloudWatch Logs（5GB/月） | $2.5 |
| ECR（10GB） | $1 |
| Supabase Free | $0 |
| **合計** | **約$35/月** |

### 本番環境

| サービス | 月額 |
|---------|------|
| ECS Fargate（0.5 vCPU, 1GB, 24h × 2タスク） | $40 |
| ALB | $20 |
| CloudWatch Logs（20GB/月） | $10 |
| ECR（50GB） | $5 |
| Supabase Pro | $25 |
| データ転送 | $10 |
| **合計** | **約$110/月** |

---

## 関連ドキュメント

- [定期メンテナンス](routine-maintenance.md)
- [監視概要](../monitoring/monitoring-overview.md)
- [パフォーマンスチューニング](../troubleshooting/performance-tuning.md)

---

**最終更新日**: 2025-12-17
