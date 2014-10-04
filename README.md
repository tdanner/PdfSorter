PdfSorter
=========

I use a Fujitsu ScanSnap to deal with incoming paper. While I could probably
get away with just leaving all the scanned pdfs in the default directory, I
prefer to sort them into directories according to (more or less) who sent the
paper. This is fairly tedious with the default ScanSnap Organizer software for
a few reasons:

* The thumbnails are quite small. Some common paper statements (electricity 
  bill, etc.) are easily recognized at thumbnail size, but others are not.
* There is no keyboard command for moving the selected file to a 
  subdirectory. You have to use the mouse.
* If you have more subdirectories than will fit vertically on the screen, 
  you have to scroll.

PdfSorter is a trivial Windows desktop application that aims to simplify this
task with the following workflow:

1. View a single PDF at a reasonably large size.
2. Choose a subdirectory by type-ahead/fuzzy matching.
3. Hit enter to move the current PDF into the chosen subdirectory and select
   the next one automatically.
4. Repeat the process until done.

In the future, I'd like to add some intelligence to the subdirectory 
selection process. Maybe Bayesian classification based on text contents
and past manual classiciation?
