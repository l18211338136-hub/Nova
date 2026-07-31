using Nova.Framework.Domain.Auditing;

namespace Nova.Modules.Identity.Domain.Menus;

public class Menu : IFullAuditedEntity
{
    // 给 EF Core 使用的私有无参构造函数
    private Menu()
    {
        Id = Guid.CreateVersion7();
    }

    // 强约束的私有全参构造函数
    private Menu(string name, string path, string component, string? icon, Guid? parentId, int sort) : this()
    {
        Name = name;
        Path = path;
        Component = component;
        Icon = icon;
        ParentId = parentId;
        Sort = sort;
    }

    public Guid Id { get; private set; }
    public Guid? ParentId { get; private set; }
    
    // 菜单名称（四字规范）
    public string Name { get; private set; } = default!;
    
    // 前端路由地址
    public string Path { get; private set; } = default!;
    
    // 前端组件相对路径
    public string Component { get; private set; } = default!;
    
    // 图标
    public string? Icon { get; private set; }
    
    // 排序号
    public int Sort { get; private set; }

    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? Remarks { get; set; }
    public bool IsEnabled { get; set; } = true;

    // 领域工厂方法
    public static Menu Create(string name, string path, string component, string? icon = null, Guid? parentId = null, int sort = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentException.ThrowIfNullOrWhiteSpace(component, nameof(component));

        return new Menu(name, path, component, icon, parentId, sort);
    }

    // 更新状态
    public void Update(string name, string path, string component, string? icon, Guid? parentId, int sort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentException.ThrowIfNullOrWhiteSpace(component, nameof(component));

        Name = name;
        Path = path;
        Component = component;
        Icon = icon;
        ParentId = parentId;
        Sort = sort;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
