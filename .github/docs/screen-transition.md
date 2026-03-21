# 画面遷移図

## 概要
ASP.NET Core MVCアプリケーションの画面遷移を3つの大分類で整理

## システム構成

```mermaid
graph TD
    Start([ユーザー]) --> Home[ホーム画面<br/>/dotnet/Home/Index]

    Home --> Education[エンジニア教育用]
    Home --> CoreSystem[基幹システム]
    Home --> Pattern[デザインパターン]
    Home --> Testing[テスト技法]
    Home --> DDD[ドメイン駆動設計]
    Home --> TableDesign[テーブル設計]

    subgraph "🎓 エンジニア教育用"
        Education --> DemoPerf[SQLパフォーマンス<br/>/dotnet/Demo/Performance]
        Education --> DemoError[エラーハンドリング<br/>/dotnet/Demo/ErrorHandling]
        Education --> DemoLog[ログ<br/>/dotnet/Demo/Logging]
        Education --> DemoVal[バリデーション<br/>/dotnet/Demo/Validation]
        Education --> DemoData[データ構造<br/>/dotnet/Demo/DataStructures]
        Education --> DemoSecurity[セキュリティ<br/>/dotnet/Demo/Security]
        Education --> DemoDB[DB接続<br/>/dotnet/Demo/DatabaseConnection]
    end

    subgraph "🏢 基幹システム"
        CoreSystem --> Inventory[在庫管理<br/>/dotnet/Inventory/Index]
        CoreSystem --> Sales[販売管理<br/>/dotnet/Sales/Index]
        CoreSystem --> Production[生産管理<br/>/dotnet/Production/Index]
    end

    subgraph "🎨 デザインパターン"
        Pattern --> Singleton[Singleton<br/>/dotnet/Demo/DesignPattern/Singleton]
        Pattern --> Factory[Factory Method]
        Pattern --> Repository[Repository]
        Pattern --> Strategy[Strategy]
    end

    subgraph "🧪 テスト技法"
        Testing --> Equiv[同値分割]
        Testing --> Boundary[境界値分析]
        Testing --> Decision[デシジョンテーブル]
        Testing --> State[状態遷移テスト]
    end

    subgraph "🏗️ ドメイン駆動設計"
        DDD --> Entity[エンティティ]
        DDD --> ValueObj[値オブジェクト]
        DDD --> Aggregate[集約・リポジトリ]
    end

    subgraph "🗄️ テーブル設計"
        TableDesign --> Norm[正規化]
        TableDesign --> ER[ER図の読み方]
        TableDesign --> Index[インデックス設計]
    end

    style Home fill:#e1f5ff
    style Education fill:#ffe1e1,stroke:#ff4444,stroke-width:3px
    style CoreSystem fill:#e1ffe1,stroke:#44ff44,stroke-width:3px
    style Pattern fill:#e8e1ff,stroke:#7744ff,stroke-width:3px
    style Testing fill:#fff0e1,stroke:#ff8844,stroke-width:3px
    style DDD fill:#e1f0ff,stroke:#4488ff,stroke-width:3px
    style TableDesign fill:#e1ffee,stroke:#44bb88,stroke-width:3px
    style Inventory fill:#e8ffe8,stroke:#00aa00,stroke-width:2px,stroke-dasharray: 5 5
    style Sales fill:#f0fff0,stroke:#00aa00,stroke-dasharray: 5 5
    style Production fill:#f8fff8,stroke:#00aa00,stroke-dasharray: 5 5
```

---

## 🏠 ホーム画面

**パス**: `/dotnet/Home/Index`

### 機能
- アプリケーションのエントリーポイント
- 全セクションへのナビゲーション提供

### 表示内容
- アプリケーション紹介
- エンジニア教育用デモへのリンク
- 基幹システムへのリンク
- デザインパターン・テスト技法・DDD・テーブル設計へのリンク

---

## 🎓 エンジニア教育用

### 目的
- **将来教育担当になったときに、このデモを使って自分の負担を減らす**
  - 口頭説明より実際に動くコードで理解してもらう
  - 繰り返し使える教材として整備
  - 新人教育の時間短縮と品質向上
- データベース性能問題の学習
- エラーハンドリングのベストプラクティス習得
- データ構造とアルゴリズムの基礎理解
- セキュリティの脆弱性と対策の体験

### 実装状況
- ✅ SQLパフォーマンス（N+1, フルスキャン, SELECT *, LIKE検索）
- ✅ エラーハンドリング
- ✅ ログ
- ✅ バリデーション
- 🚧 セキュリティ
- 🚧 DB接続
- 🚧 データ構造とアルゴリズム

---

### 1️⃣ SQLパフォーマンス

**パス**: `/dotnet/Demo/Performance`
**ステータス**: ✅ 実装済み
**データベース**: SQLite（軽量・セットアップ不要）

#### 機能
N+1問題のデモと最適化手法の比較

#### デモ内容
| パターン | クエリ数 | 実装方法 |
|---------|---------|---------|
| ❌ Bad | 101回 | ループ内でクエリ実行 |
| ✅ Good | 1回 | JOINクエリで一括取得 |

#### 学習ポイント
- N+1問題の発生原因
- JOINによる最適化
- 実行時間・クエリ回数の測定
- 素のSQL（ADO.NET）の書き方

#### API
- `GET /api/demo/n-plus-one/bad` - 非効率版
- `GET /api/demo/n-plus-one/good` - 最適化版

---

### 2️⃣ エラーハンドリング

**パス**: `/dotnet/Demo/ErrorHandling`
**ステータス**: ✅ 実装済み

#### 機能
例外処理のベストプラクティスデモ

#### デモ内容
- ❌ **Bad**: try-catch乱用、例外握りつぶし
- ✅ **Good**: 適切な例外処理、カスタム例外、ログ出力
- リトライ戦略（Exponential Backoff）
- Circuit Breaker パターン

#### 学習ポイント
- try-catch-finally の正しい使い方
- 例外の種類と使い分け
- カスタム例外の設計
- ログ出力のベストプラクティス

---

### 3️⃣ セキュリティ

**パス**: `/dotnet/Demo/Security`
**ステータス**: 🚧 未実装

#### 機能
OWASP Top 10に基づく脆弱性デモ

#### デモ内容

| 脆弱性 | Vulnerable | Secure |
|-------|-----------|--------|
| SQLインジェクション | 文字列連結 | パラメータ化クエリ |
| XSS | 生出力 | HTMLエンコード + CSP |
| CSRF | トークンなし | Anti-CSRFトークン |

#### 学習ポイント
- OWASP Top 10の理解
- SQLインジェクションの仕組みと対策
- XSSの種類（Reflected, Stored, DOM-based）
- セキュアコーディングの原則

---

### 4️⃣ データ構造とアルゴリズム

**パス**: `/dotnet/Demo/DataStructures`
**ステータス**: 🚧 未実装

#### 機能
データ構造のパフォーマンス比較デモ

#### デモ内容
- **検索**: List.Contains() vs HashSet.Contains()
- **ソート**: バブルソート vs クイックソート
- **選択**: List vs LinkedList、Dictionary vs SortedDictionary

#### 学習ポイント
- 時間計算量（Time Complexity）
- Big O記法（O(1), O(n), O(log n), O(n²)）
- ハッシュテーブルの仕組み
- 適切なデータ構造の選択基準

---

## 🏢 基幹システム

### 目的
- 業務システムの基本機能構築
- 外部サービス連携の実装
- システム監視とヘルスチェック

### 実装予定順序
1. **在庫管理** ← 次に実装
2. **販売管理**
3. **生産管理**

---

### 1️⃣ 在庫管理

**パス**: `/dotnet/Inventory/Index`
**ステータス**: 🚧 未実装（次に実装予定）

#### 機能（予定）
- 商品マスタ管理
- 在庫数量管理
- 入出庫履歴
- 在庫アラート

---

### 2️⃣ 販売管理

**パス**: `/dotnet/Sales/Index`
**ステータス**: 🚧 未実装

#### 機能（予定）
- 売上伝票入力
- 請求書発行
- 売上集計レポート

---

### 3️⃣ 生産管理

**パス**: `/dotnet/Production/Index`
**ステータス**: 🚧 未実装

#### 機能（予定）
- 生産計画立案
- 製造指示書発行
- 進捗管理

---

## 🎨 デザインパターン編

**ステータス**: 🚧 全項目未実装

| 画面 | パス |
|-----|------|
| Singleton | /dotnet/Demo/DesignPattern/Singleton |
| Factory Method | /dotnet/Demo/DesignPattern/FactoryMethod |
| Repository | /dotnet/Demo/DesignPattern/Repository |
| Strategy | /dotnet/Demo/DesignPattern/Strategy |
| Observer | /dotnet/Demo/DesignPattern/Observer |
| Decorator | /dotnet/Demo/DesignPattern/Decorator |
| Command | /dotnet/Demo/DesignPattern/Command |

---

## 🧪 テスト技法編

**ステータス**: 🚧 全項目未実装

| 画面 | パス |
|-----|------|
| 同値分割 | /dotnet/Demo/TestingTechniques/EquivalencePartitioning |
| 境界値分析 | /dotnet/Demo/TestingTechniques/BoundaryValue |
| デシジョンテーブル | /dotnet/Demo/TestingTechniques/DecisionTable |
| 状態遷移テスト | /dotnet/Demo/TestingTechniques/StateTransition |

---

## 🏗️ ドメイン駆動設計編

**ステータス**: 🚧 全項目未実装

| 画面 | パス |
|-----|------|
| エンティティ | /dotnet/Demo/DDD/Entity |
| 値オブジェクト | /dotnet/Demo/DDD/ValueObject |
| 集約・リポジトリ | /dotnet/Demo/DDD/Aggregate |

---

## 🗄️ テーブル設計編

**ステータス**: 🚧 全項目未実装

| 画面 | パス |
|-----|------|
| 正規化 | /dotnet/Demo/TableDesign/Normalization |
| ER図の読み方 | /dotnet/Demo/TableDesign/ERDiagram |
| インデックス設計 | /dotnet/Demo/TableDesign/Index |

---

## 🛠️ 技術スタック

### フロントエンド
- Razor Views (MVC)
- HTML, CSS, JavaScript

### バックエンド
- ASP.NET Core 8.0 MVC
- ADO.NET (Raw SQL)

### データベース
- **SQLite**: 教育用デモ（軽量・セットアップ不要）
- **postgres**: 基幹システム（LocalDB for development）

### インフラ
- AWS ECS Fargate
- AWS Secrets Manager
- Supabase

---

## 📝 備考

### レイアウト
- 全画面共通レイアウト: `_Layout.cshtml`
- グローバルナビゲーションバー常時表示

### API設計
- REST API: JSON形式レスポンス
- デモ画面: JavaScriptでAPI呼び出し、動的表示

### 命名規則
- MVC View: `/dotnet/{Feature}/Index`
- REST API: `/api/{feature}/{action}`

### ステータス凡例
- ✅ 実装済み
- 🚧 未実装（計画中）
- ← 次に実装予定
