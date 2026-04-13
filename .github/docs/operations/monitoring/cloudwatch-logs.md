# CloudWatch Logs 運用ガイド

## 概要

ECSタスクの標準出力が CloudWatch Logs に自動転送される。  
このドキュメントはログの確認・検索手順を記載する。

> **インフラ設定**（保持期間・メトリクスフィルタ・アラーム）は `my_web_infra` の Terraform で管理する。

---

## ログ構成

### ロググループ

| ロググループ | 用途 |
|---|---|
| `/ecs/dotnet-app` | .NETアプリケーションのログ |
| `/aws/ecs/containerinsights/app-cluster/performance` | Container Insights |

### ログストリーム

ECSタスクごとに自動作成される。

命名規則: `ecs/web/<task-id>`

例: `ecs/web/55e1feef8e214df7b5d5111fe40a49e1`

---

## ログ確認方法

### AWS Management Console

1. CloudWatch Console を開く
2. 左メニュー「ログ」→「ロググループ」を選択
3. `/ecs/dotnet-app` をクリック
4. 最新のログストリームを選択
5. フィルタ機能で検索

> 📸 **スクショ①**: ロググループ一覧に `/ecs/dotnet-app` が表示されている画面

> 📸 **スクショ②**: ログストリームを開いてアプリのログが表示されている画面

### AWS CLI

```bash
# 最新10分間のログを表示
MSYS_NO_PATHCONV=1 aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --region ap-northeast-1 \
  --output table \
  --query "events[*].[timestamp,message]"
```

> 📸 **スクショ③**: 上記コマンドの実行結果

---

## ログ検索

### キーワード検索

```bash
# ERRORログのみ表示（過去24時間）
MSYS_NO_PATHCONV=1 aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "ERROR" \
  --start-time $(date -d '24 hours ago' +%s)000 \
  --region ap-northeast-1

# 例外・スタックトレース（過去24時間）
MSYS_NO_PATHCONV=1 aws logs filter-log-events \
  --log-group-name /ecs/dotnet-app \
  --filter-pattern "Exception" \
  --start-time $(date -d '24 hours ago' +%s)000 \
  --region ap-northeast-1
```

### Logs Insights（コンソールで実行）

```sql
-- 直近のERRORログ
fields @timestamp, @message
| filter @message like /ERROR/
| sort @timestamp desc
| limit 100
```

```sql
-- 時系列エラー件数（5分単位）
fields @timestamp, @message
| filter @message like /ERROR/
| stats count() by bin(5m)
```

---

## トラブルシューティング

### ログが出力されない場合

#### ECSタスク定義の確認

```bash
aws ecs describe-task-definition \
  --task-definition dotnet-app \
  --region ap-northeast-1 \
  --query "taskDefinition.containerDefinitions[0].logConfiguration"
```

期待される出力:

```json
{
    "logDriver": "awslogs",
    "options": {
        "awslogs-group": "/ecs/dotnet-app",
        "awslogs-region": "ap-northeast-1",
        "awslogs-stream-prefix": "ecs"
    }
}
```

---

## 関連ドキュメント

- [監視概要](monitoring-overview.md)
- [メトリクス監視](metrics.md)

---

**最終更新日**: 2026-04-13
