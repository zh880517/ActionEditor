using System.Collections.Generic;

namespace EasyConfig
{
    public interface IEntityConfig
    {
        List<IConfigComponent> Components { get; }
    }
}
