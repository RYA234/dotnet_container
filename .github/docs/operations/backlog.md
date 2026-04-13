# 着手リスト（バックログ）

## 運用・インフラ

| No | 機能名 | 設計書 | 状態 | 優先度 |
|---|---|---|---|---|
| O-01 | デプロイ手順書整備 | [deployment/](./deployment/) | 🔄 レビュー中 | 高 |
| O-02 | 監視・アラート設計 | [monitoring/](./monitoring/) | 🔄 レビュー中 | 高 |
| O-03 | バックアップ設計 | [backup-recovery/](./backup-recovery/) | 🔄 レビュー中 | 中 |
| O-04 | トラブルシューティング整備 | [troubleshooting/](./troubleshooting/) | 🔄 レビュー中 | 中 |
| O-05 | 定期メンテナンス手順書 | [maintenance/](./maintenance/) | 🔄 レビュー中 | 低 |

---

## Issue化方針

1ファイル1Issueで管理する。

### deployment/

| Issue | ファイル |
|-------|---------|
| [OPS-01] automated-deployment.md レビュー・実績記録 | automated-deployment.md |
| [OPS-02] manual-deployment.md レビュー・実績記録 ([#80](https://github.com/RYA234/dotnet_container/issues/80)) | manual-deployment.md |
| [OPS-03] rollback.md レビュー・実績記録 | rollback.md |
| [OPS-04] deployment-checklist.md レビュー・実績記録 | deployment-checklist.md |

### monitoring/

| Issue | ファイル |
|-------|---------|
| [OPS-05] monitoring-overview.md レビュー | monitoring-overview.md |
| [OPS-06] cloudwatch-logs.md レビュー・実績記録 ([#81](https://github.com/RYA234/dotnet_container/issues/81)) | cloudwatch-logs.md |
| [OPS-07] health-checks.md レビュー・実績記録 ([#82](https://github.com/RYA234/dotnet_container/issues/82)) | health-checks.md |
| [OPS-08] metrics.md レビュー・実績記録 | metrics.md |

### backup-recovery/

| Issue | ファイル |
|-------|---------|
| [OPS-10] backup-strategy.md レビュー | backup-strategy.md |
| [OPS-11] database-backup.md レビュー・実績記録 | database-backup.md |
| [OPS-12] disaster-recovery.md レビュー・実績記録 | disaster-recovery.md |

### troubleshooting/

| Issue | ファイル |
|-------|---------|
| [OPS-13] common-issues.md レビュー | common-issues.md |
| [OPS-14] log-analysis.md レビュー・実績記録 | log-analysis.md |
| [OPS-15] performance-tuning.md レビュー | performance-tuning.md |

### maintenance/

| Issue | ファイル |
|-------|---------|
| [OPS-16] routine-maintenance.md レビュー・実績記録 | routine-maintenance.md |
| [OPS-17] security-updates.md レビュー・実績記録 | security-updates.md |
| [OPS-18] cost-optimization.md レビュー | cost-optimization.md |

### その他

| Issue | 内容 |
|-------|------|
| [OPS-19] CONTRIBUTING.md 作成 | 新規作成 |
| [OPS-20] 運用設計の現職フォーマット見直し | 全体見直し |

---

## スケジュール

### 2週間以内（〜2026-04-04）

#### ドキュメントレビュー（読むだけ）

| No | タスク | Issue |
|---|---|---|
| 1 | deployment/ 全ファイルを読んで内容が実態と合っているか確認 | OPS-01 |
| 2 | monitoring/ 全ファイルを読んで内容が実態と合っているか確認 | OPS-02 |
| 3 | 実態と合っていない箇所をコメントで残す | OPS-01 / OPS-02 |

#### 手を動かして確認（スクショ・実績を手順書に貼る）

| No | タスク | Issue |
|---|---|---|
| 4 | デプロイを実際に実行してスクショを手順書に貼る | OPS-01 |
| 5 | CloudWatch Logsでログ確認手順を実行してスクショを貼る | OPS-02 |
| 6 | ヘルスチェックURLにアクセスして結果をスクショで残す | OPS-02 |
| 7 | CONTRIBUTING.md を作成 | OPS-03 |

---

### 1ヶ月以降（2026-04-21〜）

#### ドキュメントレビュー（読むだけ）

| No | タスク | Issue |
|---|---|---|
| 1 | backup-recovery/ 全ファイルを読んで実態と照合 | OPS-04 |
| 2 | troubleshooting/ 全ファイルを読んで実態と照合 | OPS-05 |
| 3 | maintenance/ 全ファイルを読んで実態と照合 | OPS-05 |

#### 手を動かして確認（スクショ・実績を手順書に貼る）

| No | タスク | Issue |
|---|---|---|
| 4 | バックアップを実際に実行してスクショを貼る | OPS-04 |
| 5 | ロールバック手順を実行してスクショを貼る | OPS-04 |
| 6 | 運用設計を現職フォーマットに合わせて見直し | OPS-06 |
