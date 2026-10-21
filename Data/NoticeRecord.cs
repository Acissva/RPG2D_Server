namespace RPG2D_Server.Data
{
    /*  record:
        C# 9 新增的语法，用来快速定义**值语义的数据载体**。
        默认不带 `struct` 的 `record` → 是**引用类型（基于 class）**；
        `record struct` 才是值类型（struct）。

        不是方法：没有返回值、没有方法体，是类型定义
        是一种特殊 class（引用类型 record），主打用来存数据、做 DTO、消息载体
     */
    public sealed record NoticeRecord(long id, string Title, string Content);
}
