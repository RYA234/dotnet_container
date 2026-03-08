# GitHub Projects ブラウザ設定ガイド

## 📍 現在の状態
- **Project URL**: https://github.com/users/RYA234/projects/3
- **Project名**: Full Stack Development 2025
- **ステータス**: プロジェクト作成済み、カスタムフィールドとIssue追加が必要

---

## ステップ1: カスタムフィールドを追加（5分）

### 1-1. Projectページを開く
1. ブラウザで https://github.com/users/RYA234/projects/3 を開く
2. プロジェクトが表示されます（現在は空の状態）

### 1-2. Settings（設定）を開く
1. 画面右上の **⚙️ アイコン（Settings）** をクリック
2. または画面右上の **︙（3点メニュー）** → **Settings** をクリック

### 1-3. カスタムフィールド「Repository」を追加

**手順：**
1. Settingsページで **Fields** セクションを見つける
2. **+ New field** ボタンをクリック
3. フィールド設定：
   ```
   Field type: Single select
   Field name: Repository
   ```
4. **Options（選択肢）** を追加：
   - `dotnet_container` と入力して Enter
   - `typescript-container` と入力して Enter
   - `my_web_infra` と入力して Enter
5. **Save** または **Create** をクリック

**色の設定（オプション）:**
- dotnet_container: 青色（Blue）
- typescript-container: 緑色（Green）
- my_web_infra: 黄色（Yellow）

### 1-4. カスタムフィールド「Priority」を追加

**手順：**
1. 再度 **+ New field** をクリック
2. フィールド設定：
   ```
   Field type: Single select
   Field name: Priority
   ```
3. **Options（選択肢）** を追加：
   - `High` と入力して Enter → 赤色（Red）
   - `Medium` と入力して Enter → 黄色（Yellow）
   - `Low` と入力して Enter → 緑色（Green）
4. **Save** をクリック

### 1-5. カスタムフィールド「Category」を追加

**手順：**
1. 再度 **+ New field** をクリック
2. フィールド設定：
   ```
   Field type: Single select
   Field name: Category
   ```
3. **Options（選択肢）** を追加：
   - `Education Demo`
   - `Business System`
   - `RAG/AI`
   - `UX`
   - `Infrastructure`
   - `Documentation`
   - `Testing`
   - `Deployment`
4. **Save** をクリック

### 1-6. Settingsを閉じる
- 左上の **← Back to project** または **×** で閉じる

---

## ステップ2: Issueを追加（5分）

### 方法A: プロジェクトページから一括追加（推奨）

#### 2-A-1. dotnet_container のIssueを追加
1. Projectページで **+ Add item** ボタンをクリック
2. 検索欄に `repo:RYA234/dotnet_container is:issue is:open` と入力
3. 表示された9個のIssueをすべて選択：
   - #35 Error Handling Demo
   - #36 Security Demo
   - #37 Data Structure Demo
   - #38 Inventory Management
   - #39 Sales Management
   - #40 Production Management
   - #41 Improve Design Docs
   - #42 Improve Test Coverage
   - #43 Improve CI/CD
4. **Add selected items** をクリック

#### 2-A-2. typescript-container のIssueを追加
1. 再度 **+ Add item** をクリック
2. 検索欄に `repo:RYA234/typescript-container is:issue is:open` と入力
3. 表示された2個のIssueを選択：
   - #49 RAG Feature
   - #50 UX Feature
4. **Add selected items** をクリック

#### 2-A-3. my_web_infra のIssueを追加
1. 再度 **+ Add item** をクリック
2. 検索欄に `repo:RYA234/my_web_infra is:issue is:open` と入力
3. 表示された3個のIssueを選択：
   - #1 Improve Terraform Modules
   - #2 Add Monitoring and Alerts
   - #3 Cost Optimization
4. **Add selected items** をクリック

### 方法B: 各Issueページから個別に追加

1. Issueページを開く（例: https://github.com/RYA234/dotnet_container/issues/35）
2. 右サイドバーの **Projects** をクリック
3. **Recent** または **Search projects** で `Full Stack Development 2025` を選択
4. Issueがプロジェクトに追加されます
5. すべてのIssue（14個）に対して繰り返す

---

## ステップ3: フィールド値を設定（5分）

各Issueに対して、Repository、Priority、Categoryを設定します。

### 3-1. Table Viewに切り替え
1. プロジェクト画面左上の **View** ドロップダウンをクリック
2. **Table** を選択（カンバンではなくテーブル表示）

### 3-2. フィールド値を一括設定

**Repository列：**
- dotnet_container のIssue → `dotnet_container` を選択
- typescript-container のIssue → `typescript-container` を選択
- my_web_infra のIssue → `my_web_infra` を選択

**Priority列：**
- #35, #36, #49, #50, #2 → `High`
- #37, #38, #42, #43, #1, #3 → `Medium`
- #39, #40, #41 → `Low`

**Category列：**
- #35, #36, #37 → `Education Demo`
- #38, #39, #40 → `Business System`
- #49 → `RAG/AI`
- #50 → `UX`
- #1, #2, #3 → `Infrastructure`
- #41 → `Documentation`
- #42 → `Testing`
- #43 → `Deployment`

**設定方法：**
1. 各Issueの行で、該当する列のセルをクリック
2. ドロップダウンから適切な値を選択
3. 自動保存されます

---

## ステップ4: Viewをカスタマイズ（2分）

### 4-1. Board Viewを作成
1. 左上の **+ New view** をクリック
2. **Board** を選択
3. View名: `By Status`
4. **Group by**: `Status`
5. **Create** をクリック

### 4-2. Priority View を作成
1. **+ New view** をクリック
2. **Table** を選択
3. View名: `By Priority`
4. **Sort by**: `Priority` → `High to Low`
5. **Create** をクリック

### 4-3. Repository View を作成
1. **+ New view** をクリック
2. **Board** を選択
3. View名: `By Repository`
4. **Group by**: `Repository`
5. **Create** をクリック

---

## ステップ5: 自動化を設定（オプション、2分）

### 5-1. Workflows を開く
1. プロジェクトページで **⚙️ Settings** をクリック
2. 左サイドバーの **Workflows** をクリック

### 5-2. 自動追加ワークフローを有効化
1. **Auto-add to project** を見つける
2. **Edit** をクリック
3. 設定：
   ```
   When: Issues are opened
   Repository: All repositories
   Filter: is:open
   ```
4. **Save and turn on workflow** をクリック

### 5-3. 自動完了ワークフローを有効化
1. **Item closed** を見つける
2. **Edit** をクリック
3. 設定：
   ```
   When: Issues are closed
   Set: Status = Done
   ```
4. **Save and turn on workflow** をクリック

---

## 完了チェックリスト

- [ ] プロジェクトを開いた（https://github.com/users/RYA234/projects/3）
- [ ] カスタムフィールド「Repository」を追加（3つの選択肢）
- [ ] カスタムフィールド「Priority」を追加（3つの選択肢）
- [ ] カスタムフィールド「Category」を追加（8つの選択肢）
- [ ] 14個のIssueをプロジェクトに追加
- [ ] 各Issueに Repository, Priority, Category を設定
- [ ] 複数のViewを作成（Board, Priority, Repository）
- [ ] 自動化ワークフローを有効化（オプション）

---

## トラブルシューティング

### Q: カスタムフィールドが見つからない
**A:** Settings → Fields セクションを確認。なければページをリロード。

### Q: Issueが検索で出てこない
**A:** 検索クエリを確認：`repo:owner/repo is:issue is:open`

### Q: フィールド値が保存されない
**A:** ページをリロードして再試行。ネットワーク接続を確認。

### Q: Viewが作成できない
**A:** 既存のViewを削除してから再作成。

---

## 次のアクション

設定完了後：
1. Board Viewで全体を俯瞰
2. Priority Viewで優先タスクを確認
3. 最初のタスク（#35 Error Handling Demo）に着手

**Happy Project Management! 🎉**
