# ドキュメント一覧

このディレクトリには、プロジェクトの設計・運用に関するドキュメントが含まれています。

## 📋 目次

### 1. 要件定義・設計書（システム全体）
- [要件定義書](requirements.md) - システムの要件と目的
- [アーキテクチャ](architecture.md) - 全体構成・技術スタック
- [画面一覧](screen-list.md) / [画面詳細](screen-detail.md) - 画面設計
- [API設計](api-design.md) / [API仕様書](api-specification.md) - API仕様
- [バリデーション設計](validation.md) / [エラーハンドリング設計](error-handling.md)
- [設定管理設計](configuration.md) / [DB接続設計](database-connection.md)
- [セキュリティ設計](security.md) / [ログ設計](logging.md)
- [運用設計手順書](operations.md) - デプロイ、監視、障害対応手順

### 2. 機能別設計書 ⭐ NEW
- [機能別設計書](features/) ✅ - 機能ごとの詳細設計
  - [テンプレート](features/template/) - 新機能作成時のテンプレート
  - [N+1問題デモ](features/n-plus-one-demo/) ✅ - 実装済み（参考例）
  - エラーハンドリングデモ（未実装）
  - セキュリティデモ（未実装）
  - 在庫管理（未実装）

### 3. 画面遷移・仕様
- [画面遷移図](screen-transition.md) - システム全体の画面遷移とAPI構成

### 4. 技術判断記録 (ADR)
- [ADR一覧](adr/) - Architecture Decision Records
  - [ADRテンプレート](adr/template.md)
  - [ADR-001: SQLiteを教育用デモに採用](adr/001-use-sqlite-for-education.md)
  - [ADR-002: ORMを使わず素のSQLを採用](adr/002-avoid-orm-use-raw-sql.md)

## 📖 ドキュメントの読み方

### 新規メンバー向け
1. [要件定義書](requirements.md) - まずシステムの目的を理解
2. [画面遷移図](screen-transition.md) - システム構成を把握
3. [API設計](api-design.md) - 画面とAPIの仕様を確認

### 開発者向け（全体把握）
1. [アーキテクチャ](architecture.md) - システム全体の実装詳細
2. [ADR一覧](adr/) - 技術選定の背景を理解

### 開発者向け（機能実装）
1. [機能別設計書](features/) - 実装する機能の詳細設計
2. [N+1問題デモ](features/n-plus-one-demo/) - 実装済みの参考例
3. [テンプレート](features/template/) - 新機能作成時に使用

### 運用担当者向け
1. [運用設計手順書](operations.md) - デプロイと運用手順を確認

## 🔄 ドキュメント更新ルール

- 設計変更時は該当ドキュメントを更新
- 技術選定時はADRを作成
- 変更履歴を各ドキュメント内に記録
