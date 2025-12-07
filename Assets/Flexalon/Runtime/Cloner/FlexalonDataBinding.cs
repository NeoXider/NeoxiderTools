namespace Flexalon
{
    /// <summary>
    /// When the Cloner creates objects from a DataSource, it will search the cloned objects
    /// for any component which implements DataBinding to bind the data entry from the
    /// data source to the visual item. The component can then use this data to change its appearance.
    /// </summary>
    public interface DataBinding
    {
        /// <summary> Called when the data is set for this item. </summary>
        /// <param name="data"> The data to bind to the item. </param>
        void SetData(object data);
    }
}