-- 首次创建专用；使用 root 管理连接，确认 rpg2d_app 尚不存在。
-- 逐条执行：CREATE USER 会返回随机密码，先在本机妥善保存，再执行 GRANT。
-- 文件中不填入真实密码；生成密码的结果不复制到仓库或聊天中。
CREATE USER 'rpg2d_app'@'127.0.0.1' IDENTIFIED BY RANDOM PASSWORD REQUIRE SSL;
GRANT SELECT, INSERT, UPDATE, DELETE ON rpg2d_dev.notices TO 'rpg2d_app'@'127.0.0.1';
SHOW GRANTS FOR 'rpg2d_app'@'127.0.0.1';
