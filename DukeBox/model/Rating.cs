using System;
using System.Collections.Generic;
using System.Text;

namespace DukeBox.model; 

public class Rating{
    public int rating;
    public string label;
    public string svgPath;
    private Rating(int rating, string label, string svgPath) {
        this.rating = rating;
        this.label = label;
        this.svgPath = svgPath;
    }

    public static Rating Lowest = new Rating(1, "Lowest", "M 10 7V 9H 12V 17H 14V 7H 10Z");
    public static Rating Low = new Rating(2, "Low", "M 9 7V 9H 13V 11H 11A 2,2,0,0,0,9,13V 17H 11L 15 17V 15H 11V 13H 13A 2,2,0,0,0,15,11V 9A 2,2,0,0,0,13,7H 9Z");
    public static Rating Normal = new Rating(3, "Normal", "M 15 15V 13.5A 1.5,1.5,0,0,0,13.5,12 1.5,1.5,0,0,0,15,10.5V 9C 15 7.89 14.1 7 13 7H 9V 9H 13V 11H 11V 13H 13V 15H 9V 17H 13A 2,2,0,0,0,15,15");
    public static Rating High = new Rating(4, "High", "M 9 7V 13H 13V 17H 15V 7H 13V 11H 11V 7H 9Z");
    public static Rating Highest = new Rating(5, "Highest", "M 9 7V 13H 13V 15H 9V 17H 13A 2,2,0,0,0,15,15V 13A 2,2,0,0,0,13,11H 11V 9H 15V 7H 9Z");

    public static Rating[] List = [Lowest, Low, Normal, High, Highest];
}
