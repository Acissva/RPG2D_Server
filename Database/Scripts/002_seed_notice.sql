-- 前置：001 已执行成功，rpg2d_dev.notices 为空，当前会话为自动提交。
-- 先 SELECT 确认状态；此 INSERT 只在首次准备空表时手工执行一次。
-- 重跑 INSERT 会再次新增公告，不用于服务启动时的自动初始化。
INSERT INTO rpg2d_dev.notices (title, content) VALUES ('服务器公告', '版本更新动态：v0.0.17 正在学习 MySQL 持久化。');
