namespace che_system.model
{
    public class Status_Cards_Model
    {
        public string Title { get; set; }
        public string Icon { get; set; } // could be FontAwesome/Glyph or image path
        public int Value { get; set; }
        public string Description { get; set; } // optional (e.g., "5 chemicals below threshold")
        public string NavigationTarget { get; set; } // name of module/page to navigate
    }
}
