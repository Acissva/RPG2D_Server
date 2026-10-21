-- 首次初始化专用：目标数据库 rpg2d_dev 必须尚不存在。
-- 使用本机管理账号，逐条执行；任一步失败先核对现状，不删除已有库表重做。
CREATE DATABASE rpg2d_dev CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE rpg2d_dev;
CREATE TABLE rpg2d_dev.notices (id BIGINT NOT NULL AUTO_INCREMENT, title VARCHAR(64) NOT NULL, content VARCHAR(512) NOT NULL, PRIMARY KEY (id)) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
