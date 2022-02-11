// See https://aka.ms/new-console-template for more information

using MusicXmlBeautifier;

Console.WriteLine("***MusicXmlBeautifier***\n\n" +
    "This programm will add bass lyrics to the given musicxml file.");

Beautifier.AddBassLyrics(@"C:\Users\Dustin\Desktop\MusicXmls\SadnessAndSorrowLäufe.musicxml");
Console.Read();