namespace Asionyx.Library.Core
{
    public interface ISystemConfigurator
    {
        string GetInfo();
        void ApplyConfiguration(string json);
    }
}