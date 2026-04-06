# プロジェクト管理

## 文書情報
- **最終更新**: 2026-04-06

---

## 1. プロジェクト概要

| 項目 | 内容 |
|------|------|
| プロジェクト名 | dotnet_container |
| 開発形態 | 個人開発（1人） |
| 本番URL | https://rya234.com/dotnet |
| 目的 | 業務系エンジニア向けデモアプリの公開・スキル実証 |

---

## 2. ブランチ戦略

GitHub Flowを採用。

```
main（本番直結）
└── feature/{issue番号}-{description}
```

- `main` へのプッシュで GitHub Actions が自動デプロイ
- 作業は必ず feature ブランチで行い、PR経由でマージ

---

## 3. Issue・タスク管理

- タスクは GitHub Issues で管理
- ラベル: `enhancement` / `documentation` / `testing` / `deployment`
- GitHub Projects（Project #3）で複数リポジトリ横断管理

### Issue命名規則

```
[カテゴリ] タイトル

例:
[FEATURE] Commandパターンデモの実装
[DOC] E2Eテスト設計書のレビュー
[TEST] TestContainers導入準備
[OPS] health-checks.md レビュー
```

---

## 4. コミット規約

```
{type}: #{issue番号} {内容}

例:
feat: #88 Commandパターンデモの実装
docs: #91 E2Eテスト設計書のレビュー
fix: #94 Phaser.jsの型エラーを修正
```

---

## 5. ドキュメント構成

```
.github/docs/
├── README.md                    # ドキュメントインデックス
├── requirements.md              # 要件定義
├── architecture.md              # アーキテクチャ
├── project-management.md        # 本書
├── adr/                         # Architecture Decision Records
├── common/                      # 共通設計書（テスト・API等）
├── features/                    # 機能別設計書
└── operations/                  # 運用・開発環境
    ├── local-dev-setup.md       # ローカル開発環境構築
    ├── deployment/              # デプロイ手順
    ├── monitoring/              # 監視
    ├── troubleshooting/         # トラブルシューティング
    └── backup-recovery/         # バックアップ・リカバリ
```

---

## 6. 参考

- [ローカル開発環境セットアップ](operations/local-dev-setup.md)
- [デプロイ手順](operations/deployment/manual-deployment.md)
- [GitHub Projects](https://github.com/users/RYA234/projects/3)
