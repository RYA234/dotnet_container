# 3リポジトリ横断管理：GitHub Projects セットアップガイド

## 作成済みのIssue

### dotnet_container (9 issues)
- [#35](https://github.com/RYA234/dotnet_container/issues/35) Error Handling Demo (High)
- [#36](https://github.com/RYA234/dotnet_container/issues/36) Security Demo (High)
- [#37](https://github.com/RYA234/dotnet_container/issues/37) Data Structure Demo (Medium)
- [#38](https://github.com/RYA234/dotnet_container/issues/38) Inventory Management (Medium)
- [#39](https://github.com/RYA234/dotnet_container/issues/39) Sales Management (Low)
- [#40](https://github.com/RYA234/dotnet_container/issues/40) Production Management (Low)
- [#41](https://github.com/RYA234/dotnet_container/issues/41) Improve Design Docs (Low)
- [#42](https://github.com/RYA234/dotnet_container/issues/42) Improve Test Coverage (Medium)
- [#43](https://github.com/RYA234/dotnet_container/issues/43) Improve CI/CD (Medium)

### typescript-container (2 issues)
- [#49](https://github.com/RYA234/typescript-container/issues/49) RAG Feature (High)
- [#50](https://github.com/RYA234/typescript-container/issues/50) UX Feature (High)

### my_web_infra (3 issues)
- [#1](https://github.com/RYA234/my_web_infra/issues/1) Improve Terraform Modules (Medium)
- [#2](https://github.com/RYA234/my_web_infra/issues/2) Add Monitoring and Alerts (High)
- [#3](https://github.com/RYA234/my_web_infra/issues/3) Cost Optimization (Medium)

**合計: 14 issues**

---

## GitHub Projects 作成手順（手動）

### Step 1: Projectを作成

1. https://github.com/users/RYA234/projects にアクセス
2. **New project** ボタンをクリック
3. **Project name**: `Full Stack Development 2025`
4. **Template**: "Team backlog" を選択
5. **Create project** をクリック

### Step 2: カスタムフィールドを追加

Projectを開いたら、右上の **⚙️ Settings** をクリック：

#### フィールド1: Repository
- **Type**: Single select
- **Options**:
  - 🟦 dotnet_container
  - 🟩 typescript-container
  - 🟨 my_web_infra

#### フィールド2: Priority
- **Type**: Single select
- **Options**:
  - 🔴 High
  - 🟡 Medium
  - 🟢 Low

#### フィールド3: Category
- **Type**: Single select
- **Options**:
  - 🎓 Education Demo
  - 🏢 Business System
  - 🤖 AI/RAG
  - 🎨 UX
  - 🏗️ Infrastructure
  - 📚 Documentation
  - 🧪 Testing
  - 🚀 Deployment

### Step 3: Issueを追加

1. Project画面で **+ Add item** をクリック
2. 検索欄で `repo:RYA234/dotnet_container` と入力
3. 表示されたIssueをすべて選択して追加
4. 同様に `repo:RYA234/typescript-container` と `repo:RYA234/my_web_infra` を追加

または：

- 各Issue画面で右サイドバーの **Projects** → **Full Stack Development 2025** を選択

### Step 4: Viewを作成

#### Board View (カンバンボード)
- **Group by**: Status
- **Columns**: Todo, In Progress, Done

#### Table View (優先度順)
- **Sort by**: Priority (High → Low)
- **Filter**: Category, Repository

#### Roadmap View (タイムライン)
- **Group by**: Category
- **Timeline**: Start date / Target date

---

## 推奨ワークフロー

### 1. 毎日のルーティン
- Board Viewで "In Progress" を確認
- 完了したタスクを "Done" に移動

### 2. 週次レビュー
- Table Viewで優先度順にソート
- 次週のタスクを "Todo" → "In Progress" に移動

### 3. 月次計画
- Roadmap Viewで全体スケジュールを確認
- マイルストーンを設定

---

## 自動化設定（オプション）

Project Settingsの **Workflows** タブで以下を設定：

### Auto-add to project
- **Trigger**: Issue opened
- **Action**: Add to project

### Auto-archive closed issues
- **Trigger**: Issue closed
- **Action**: Set status to "Done" and archive

### Auto-set status
- **When**: Pull request merged
- **Set**: Status = "Done"

---

## クイックアクセス

- **Project URL**: https://github.com/users/RYA234/projects
- **dotnet Issues**: https://github.com/RYA234/dotnet_container/issues
- **typescript Issues**: https://github.com/RYA234/typescript-container/issues
- **infra Issues**: https://github.com/RYA234/my_web_infra/issues

---

## 次のアクション

✅ **今すぐやること：**
1. https://github.com/users/RYA234/projects にアクセス
2. 上記手順でProjectを作成
3. 14個のIssueを追加
4. 優先度の高いタスク（High）から着手

🎯 **優先タスク Top 5:**
1. [#35] Error Handling Demo (dotnet)
2. [#36] Security Demo (dotnet)
3. [#49] RAG Feature (typescript)
4. [#50] UX Feature (typescript)
5. [#2] Add Monitoring and Alerts (infra)
