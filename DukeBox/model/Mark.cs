namespace DukeBox.model;

public class Mark {
    public int mark;
    public string label;
    public string svgPath;
    private Mark(int mark, string label, string svgPath) {
        this.mark = mark;
        this.label = label;
        this.svgPath = svgPath;
    }

    public static Mark Star = new Mark(1, "Star", "M12,17.27L18.18,21L16.54,13.97L22,9.24L14.81,8.62L12,2L9.19,8.62L2,9.24L7.45,13.97L5.82,21L12,17.27Z");
    public static Mark Flag = new Mark(2, "Flag", "M14.4,6L14,4H5V21H7V14H12.6L13,16H20V6H14.4Z");
    public static Mark Heart = new Mark(3, "Heart", "M12,21.35L10.55,20.03C5.4,15.36 2,12.27 2,8.5C2,5.41 4.42,3 7.5,3C9.24,3 10.91,3.81 12,5.08C13.09,3.81 14.76,3 16.5,3C19.58,3 22,5.41 22,8.5C22,12.27 18.6,15.36 13.45,20.03L12,21.35Z");


    public static Mark[] List = [Star, Flag, Heart];
}
