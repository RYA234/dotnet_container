# ADR-005: Repository パターンを導入し DB 接続をデータアクセス層に限定する

## 文書情報
- **作成日**: 2026-03-07
- **バージョン**: 1.0
- **ステータス**: 採用

---

## 背景

当初の設計では Service クラスが `IConfiguration` を直接 DI して接続文字列を取得し、SQL を実行していた。

```
Controller → Service（IConfiguration で接続文字列取得 + SQL実行）→ DB
```

このプロジェクトは SQLアンチパターンのデモに加えてミニ基幹システムの実装も視野に入れている。基幹システムになるにつれて以下の問題が顕在化することが見込まれた。

| 問題 | 具体例 |
|-----|-------|
| DB接続コードの散在 | `IConfiguration` を使った `GetConnection()` が全 Service に重複する |
| 責務の混在 | Service がビジネスロジックとDB接続の両方の責務を持つ |
| セキュリティリスク | Service が接続文字列に直接アクセスできる状態が続く |
| テストの困難さ | Service をテストするだけで DB 接続のモックが必要になる |

---

## 決定

**Repository パターンを導入し、DB接続と SQL 実行の責務をデータアクセス層（Repository）に限定する。**

```
Controller → Service（ビジネスロジックのみ）→ Repository（DB接続 + SQL実行）→ DB
```

各機能（Feature）に Repository インターフェースと実装を配置する。

```
Features/Demo/
├── Services/
│   ├── INPlusOneService.cs       ← IConfiguration を持たない
│   └── NPlusOneService.cs        ← IConfiguration を持たない
└── Repositories/
    ├── INPlusOneRepository.cs    ← DB操作の契約
    └── NPlusOneRepository.cs     ← IConfiguration 経由で接続文字列を取得
```

### 層の責務

| 層 | 接続文字列へのアクセス | 責務 |
|----|------------------|------|
| Controller | ✕ 禁止 | HTTP リクエスト / レスポンス |
| Service | ✕ 禁止 | ビジネスロジック・データ加工 |
| Repository | ○ 許可（`IConfiguration` 経由のみ） | SQL 実行・DB 接続管理 |
| View / HTML | ✕ 絶対禁止 | 表示のみ |

---

## 理由

1. **セキュリティ境界の明確化**: 接続文字列にアクセスできる層を Repository に限定することで、漏洩経路を最小化できる（ADR-004 と整合）
2. **責務の分離**: Service はビジネスロジックに専念し、DB 接続の詳細を知らない。差し替えやテストが容易になる
3. **DB接続コードの一元化**: 接続文字列の取得・バリデーション・接続生成を Repository に集約することで重複を防ぐ
4. **基幹システムへの対応**: テーブル・機能が増えた場合でも、Repository 層で DB 変更の影響を吸収できる

---

## 却下した選択肢

### Service が IConfiguration を直接利用する（変更前の設計）

- **却下理由**: DB接続コードが全 Service に散在する。Service がビジネスロジックと DB 接続の両方の責務を持ち、テストが困難になる。接続文字列のアクセス範囲が広く、セキュリティリスクが高い。

### DbContext（EF Core）の利用

- **却下理由**: このプロジェクトは ORM を避けて Raw SQL を使うことで SQL アンチパターンを明示的に示すことが目的のため（ADR-002 参照）。

---

## 影響

- `class-diagram.md` にデータアクセス層（Repository）を追加
- `configuration.md` のサンプルコードを Repository 版に変更
- ADR-004（シークレットアクセスポリシー）に「Repository のみ許可」を追記
- DI 登録（Program.cs）に Repository を追加する

```
builder.Services.AddScoped<INPlusOneRepository, NPlusOneRepository>();
builder.Services.AddScoped<ISelectStarRepository, SelectStarRepository>();
builder.Services.AddScoped<ILikeSearchRepository, LikeSearchRepository>();
builder.Services.AddScoped<IFullTableScanRepository, FullTableScanRepository>();
```

---

## 参考

- [クラス図](../internal-design/class-diagram.md)
- [設定管理設計](../external-design/configuration.md)
- [ADR-002: ORM を避け Raw SQL を使用する](002-avoid-orm-use-raw-sql.md)
- [ADR-004: シークレットへのアクセスは IConfiguration 経由に限定する](004-secret-access-policy.md)
