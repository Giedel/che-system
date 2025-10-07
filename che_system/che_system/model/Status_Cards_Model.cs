//-- Status_Cards_Model.cs --

using FontAwesome.Sharp;

namespace che_system.model
{
    public class Status_Cards_Model
    {
        public string? Title { get; set; }
        public IconChar Icon { get; set; } 
        public int Value { get; set; }
        public string? Description { get; set; }
        public string? NavigationTarget { get; set; } // name of module/page to navigate
    }
}
