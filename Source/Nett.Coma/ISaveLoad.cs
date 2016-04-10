namespace Nett.Coma
{
    internal interface ISaveLoad<T>
    {
        bool CanLoad();

        T Load();
        void Save(T toSave);
    }
}
