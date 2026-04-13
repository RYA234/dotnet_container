# 運用ドキュメント

## ドキュメント構成

```
operations/
├── README.md                    # このファイル
├── deployment/                  # デプロイメント
│   ├── automated-deployment.md  # GitHub Actions 自動デプロイ
│   ├── manual-deployment.md     # 手動デプロイ
│   ├── rollback.md              # ロールバック手順
│   └── deployment-checklist.md  # デプロイ前チェックリスト
├── monitoring/                  # 監視
│   ├── monitoring-overview.md   # 監視戦略の全体像
│   ├── cloudwatch-logs.md       # ログ確認・検索
│   ├── health-checks.md         # ヘルスチェック
│   ├── metrics.md               # メトリクス監視
├── backup-recovery/             # バックアップ・復旧
│   ├── backup-strategy.md       # RPO/RTO・バックアップ方針
│   ├── database-backup.md       # DB バックアップ・リストア
│   └── disaster-recovery.md     # 災害復旧手順
├── troubleshooting/             # トラブルシューティング
│   ├── common-issues.md         # よくある問題と解決方法
│   ├── log-analysis.md          # ログ解析ガイド
│   └── performance-tuning.md    # パフォーマンスチューニング
└── maintenance/                 # メンテナンス
    ├── routine-maintenance.md   # 定期メンテナンス
    ├── security-updates.md      # セキュリティアップデート
    └── cost-optimization.md     # コスト最適化
```
