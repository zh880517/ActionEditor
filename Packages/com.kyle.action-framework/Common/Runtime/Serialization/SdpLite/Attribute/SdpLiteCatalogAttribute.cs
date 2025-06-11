using System;
/*Sdp代码生成时的分类，新建分类继承 SdpLiteCatalog
* 一个类或者结构体可以支持多个分类
* 代码生成时同一分类的会成成在一个文件里面
* 每个分类可以指定目录
* 多个分类的会生成在多多份
* {SdpLiteCatalog Type名}Packer : SdpLitePacker
* {SdpLiteCatalog Type名}UnPacker : SdpLiteUnPacker
*/

public enum SdpLitePackType
{
    Normal = 1,
    Param = 2,
    All = 3,
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited= false, AllowMultiple = false)]
public abstract class SdpLiteCatalogAttribute : Attribute
{
    public abstract string GenerateRooPath { get; }//生成代码的根目录
    public abstract string NameSpace { get; }//生成的代码的命名空间
    public virtual SdpLitePackType PackType=>SdpLitePackType.Normal;//生成的代码类型
    public virtual Type PackBaseType => null;
    public virtual Type UnpackBaseType => null;
}