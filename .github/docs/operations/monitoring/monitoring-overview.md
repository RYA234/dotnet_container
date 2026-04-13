# 監視概要

## 概要

現在の監視構成をまとめる。個人開発・デモ用途のため最小構成。

> **アラーム・メトリクスフィルタ**の設定は `my_web_infra` の Terraform で管理する。

---

## 現在の監視構成

| 監視項目 | 手段 | 状態 |
|---|---|---|
| アプリの死活 | ALB ヘルスチェック（`/dotnet/healthz`） | 設定済み ✅ |
| アプリログ | CloudWatch Logs（`/ecs/dotnet-app`） | 設定済み ✅ |
| CPU・メモリ | CloudWatch Metrics（ECS標準） | 自動収集 ✅ |
| アラーム通知 | CloudWatch Alarms | **未設定** |

---

## ログの流れ

```
ECSタスク（stdout）
  ↓ awslogs ドライバー
CloudWatch Logs（/ecs/dotnet-app）
```

---

## 確認方法

各詳細は個別ドキュメントを参照。

- [CloudWatch Logs](cloudwatch-logs.md) — ログの確認・検索
- [メトリクス監視](metrics.md) — CPU・メモリ・レスポンスタイム
- [ヘルスチェック](health-checks.md) — `healthz` エンドポイントの確認

---

**最終更新日**: 2026-04-13
