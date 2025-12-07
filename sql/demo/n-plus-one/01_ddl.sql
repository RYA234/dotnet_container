-- N+1問題デモ用のテーブル作成DDL

-- 部署テーブル
CREATE TABLE Departments (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- ユーザーテーブル
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    DepartmentId INT NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);

-- インデックス作成
CREATE INDEX IX_Users_DepartmentId ON Users(DepartmentId);
