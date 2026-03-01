# エラーハンドリング設計

## 文書情報
- **文書種別**: 外部設計書
- **作成日**: 2025-12-12
- **最終更新**: 2025-12-12
- **バージョン**: 1.0
- **ステータス**: 実装中

## 文書の目的

本文書は**外部設計書**として、エラーハンドリングの構造・動作を言語非依存で定義する。

実装言語・フレームワークが決定した後、本設計書を元に実装設計書（内部設計書）およびコードを作成する。

| 設計フェーズ | 文書 | 内容 |
|---|---|---|
| 外部設計（本文書） | error-handling.md | クラス構造・処理フロー・設計意図 |
| 内部設計 | 実装時に作成 | 言語固有の実装詳細・使用例 |

---

## 1. エラーハンドリングの基本方針

### 1.1 目的

1. **安定性**: 例外発生時もアプリケーションを停止させない
2. **可読性**: エラーメッセージをユーザーにわかりやすく提示
3. **保守性**: エラーの原因を素早く特定できる
4. **セキュリティ**: 内部情報を外部に漏らさない

---

### 1.2 エラーハンドリングの原則

| 原則 | 説明 |
|------|------|
| **レイヤー分離** | Controller → Service → Infrastructure で責務を明確化 |
| **早期リターン** | エラーは早く検知して早く返す |
| **適切なログ出力** | すべてのエラーを Information/Warning/Error で記録 |
| **ユーザーフレンドリー** | 技術的詳細を隠し、対処方法を提示 |

---

## 2. エラーレスポンス形式

### 2.1 開発環境（詳細情報あり）

```json
{
  "error": "User not found",
  "code": "NOT_FOUND",
  "details": {
    "resourceType": "User",
    "resourceId": "123"
  },
  "timestamp": "2025-12-12T10:00:00.123Z",
  "stackTrace": "at UserService.GetUserById..."
}
```

---

### 2.2 本番環境（簡潔）

```json
{
  "error": "User not found",
  "code": "NOT_FOUND",
  "timestamp": "2025-12-12T10:00:00.123Z"
}
```

**形式**: JSON形式、スタックトレースは本番環境では非表示

---

## 3. 例外の種類

### 3.1 3つのカテゴリ

例外を以下の3つに分類して管理します。

#### 1️⃣ **業務上の例外**（Business Exceptions）
- **説明**: ビジネスルールやバリデーションのエラー
- **HTTPステータス**: 400 Bad Request / 404 Not Found
- **対応**: ユーザーに修正方法を提示
- **例**: 在庫不足、必須項目未入力、ユーザーID不一致

#### 2️⃣ **システム上の例外**（Infrastructure Exceptions）
- **説明**: 外部システムやインフラのエラー
- **HTTPステータス**: 500 Internal Server Error / 503 Service Unavailable
- **対応**: リトライ、アラート通知
- **例**: DB接続失敗、外部API タイムアウト

#### 3️⃣ **プログラミング言語の例外**（Runtime Exceptions）
- **説明**: ランタイムの予期しないエラー
- **HTTPステータス**: 500 Internal Server Error
- **対応**: ログ出力、バグ修正
- **例**: NullReferenceException, ArgumentNullException, IndexOutOfRangeException

---

### 3.2 カスタム例外の分類

| 例外クラス | カテゴリ | HTTPステータス | 用途 | 例 |
|-----------|---------|--------------|------|-----|
| **ValidationException** | 業務 | 400 Bad Request | 入力検証エラー | 必須項目未入力 |
| **NotFoundException** | 業務 | 404 Not Found | リソース未存在 | ユーザーID不一致 |
| **BusinessRuleException** | 業務 | 400 Bad Request | ビジネスルール違反 | 在庫不足 |
| **InfrastructureException** | システム | 500 Internal Server Error | 外部システムエラー | DB接続失敗 |

**補足**:
- ランタイム例外（NullReferenceException等）はキャッチして InfrastructureException に変換
- すべての例外は最終的に ErrorResponse として返却

---

## 4. エラーハンドリングのタイミング

### 4.1 エラーハンドリングフロー

```mermaid
flowchart TD
    Start([リクエスト受信]) --> Validate{入力検証}

    Validate -->|NG| Return400[400 Bad Request<br/>ValidationException]
    Return400 --> Log1[Warning: ログ出力]
    Log1 --> End1([レスポンス返却])

    Validate -->|OK| Service[Service層処理]
    Service --> Check{エラー発生?}

    Check -->|NotFoundException| Return404[404 Not Found]
    Return404 --> Log2[Warning: ログ出力]
    Log2 --> End2([レスポンス返却])

    Check -->|BusinessRuleException| Return400_2[400 Bad Request]
    Return400_2 --> Log3[Warning: ログ出力]
    Log3 --> End3([レスポンス返却])

    Check -->|InfrastructureException| Return500[500 Internal Server Error]
    Return500 --> Log4[Error: ログ出力]
    Log4 --> End4([レスポンス返却])

    Check -->|OK| Return200[200 OK]
    Return200 --> End5([レスポンス返却])

    style Return400 fill:#fff3cd
    style Return404 fill:#fff3cd
    style Return400_2 fill:#fff3cd
    style Return500 fill:#f8d7da
    style Return200 fill:#d4edda
```

---

## 5. 例外クラス定義

### 5.1 クラス図

```mermaid
classDiagram
    direction TB
    class Exception {
        <<system>>
    }
    class ApplicationException {
        <<abstract>>
        +string ErrorCode
        +int StatusCode
        +Dictionary~string, object~ Details
    }
    class ValidationException {
        +List~ValidationError~ Errors
    }
    class NotFoundException {
        +string ResourceType
        +string ResourceId
    }
    class BusinessRuleException {
        +string RuleName
    }
    class InfrastructureException {
        +string Service
    }

    Exception <|-- ApplicationException
    ApplicationException <|-- ValidationException
    ApplicationException <|-- NotFoundException
    ApplicationException <|-- BusinessRuleException
    ApplicationException <|-- InfrastructureException
```

### 5.2 各クラスの固有プロパティの意図

| クラス | 固有プロパティ | 用途 |
|---|---|---|
| ApplicationException | ErrorCode / StatusCode / Details | 全例外共通。エラー種別・HTTPステータス・追加情報を保持 |
| ValidationException | Errors（リスト） | 複数フィールドのエラーを同時に返すためリスト型 |
| NotFoundException | ResourceType / ResourceId | 何のリソースがどのIDで見つからなかったかを特定 |
| BusinessRuleException | RuleName | どのビジネスルールに違反したかを特定（例: CreditLimitExceeded） |
| InfrastructureException | Service | どの外部サービスが障害かを特定（例: Database・ExternalAPI） |

---

### 5.3 ValidationException（入力検証エラー）

ユーザーの入力値が不正な場合に発生する。複数フィールドのエラーをまとめて返す。

```mermaid
sequenceDiagram
    actor User
    participant API
    participant Validator

    User->>API: リクエスト送信 (Email空・パスワード短い)
    API->>Validator: 入力値を検証
    Validator-->>API: ValidationException(Errors: [Email必須, PW8文字以上])
    API-->>User: 400 Bad Request
    Note over API,User: ErrorCode: VALIDATION_ERROR<br/>Errors: 違反フィールドのリスト
```

---

### 5.4 NotFoundException（リソース未存在）

指定されたIDのリソースがDBに存在しない場合に発生する。

```mermaid
sequenceDiagram
    actor User
    participant API
    participant DB

    User->>API: GET /users/999
    API->>DB: ID=999 のユーザーを検索
    DB-->>API: 結果なし
    API-->>User: 404 Not Found
    Note over API,User: ErrorCode: NOT_FOUND<br/>ResourceType: User<br/>ResourceId: 999
```

---

### 5.5 BusinessRuleException（ビジネスルール違反）

技術的には正常だがビジネスルール上許可できない操作の場合に発生する。

```mermaid
sequenceDiagram
    actor User
    participant API
    participant RuleChecker

    User->>API: 注文リクエスト (金額: 100万円)
    API->>RuleChecker: ビジネスルール検証
    RuleChecker-->>API: BusinessRuleException(RuleName: CreditLimitExceeded)
    API-->>User: 400 Bad Request
    Note over API,User: ErrorCode: BUSINESS_RULE_VIOLATION<br/>RuleName: 違反したルール名
```

---

### 5.6 InfrastructureException（外部システムエラー）

DB・外部APIなど外部システムの障害時に発生する。

```mermaid
sequenceDiagram
    participant API
    participant DB

    API->>DB: DB接続
    DB-->>API: 接続失敗
    Note over API: InfrastructureException発生
    API-->>API: 500 Internal Server Error
    Note over API: ErrorCode: INFRASTRUCTURE_ERROR<br/>Service: Database
```

---

## 6. エラーコード一覧

### 6.1 クライアントエラー（4xx）

| コード | HTTPステータス | 説明 |
|-------|--------------|------|
| `VALIDATION_ERROR` | 400 | バリデーションエラー |
| `BUSINESS_RULE_VIOLATION` | 400 | ビジネスルール違反 |
| `UNAUTHORIZED` | 401 | 認証エラー |
| `FORBIDDEN` | 403 | 権限エラー |
| `NOT_FOUND` | 404 | リソースが見つからない |
| `CONFLICT` | 409 | リソースの競合 |
| `RATE_LIMIT_EXCEEDED` | 429 | レート制限超過 |

---

### 6.2 サーバーエラー（5xx）

| コード | HTTPステータス | 説明 |
|-------|--------------|------|
| `INTERNAL_ERROR` | 500 | 内部サーバーエラー |
| `DB_ERROR` | 500 | データベースエラー |
| `INFRASTRUCTURE_ERROR` | 500 | インフラストラクチャエラー |
| `SERVICE_UNAVAILABLE` | 503 | サービス利用不可 |

---

## 7. ログ戦略

### 7.1 ログレベルの使い分け

| レベル | 用途 | 例 |
|--------|------|-----|
| Error | 回復不可能なエラー | DB接続失敗、予期しない例外 |
| Warning | 回復可能なエラー | リトライ実行、バリデーションエラー |
| Information | 正常な処理 | ユーザー作成成功 |
| Debug | デバッグ情報 | SQL実行、変数の値 |

---

## 8. 参考

- [ログ設計](logging.md)
- [API設計規約](api-design.md)
- [セキュリティ設計](security.md)

## 9. 参考書籍

高安 厚思,『システム設計の謎を解く 改訂版』, SB Creative, 2017年, pp.251-252.
