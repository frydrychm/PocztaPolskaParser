# C# parser of pdf file distributed by polish Post Office
The parser has been written in modern C# with nuget iText (v7)  library

Simple parser, parsing pdf shared by polish Post Office including the first part of the pdf file

the parser is parsing only the first part of the document

result is csv file with columns the same as in pdf file. The created csv file column is separated by semicolons; 



# usage:

the parser (exe after build) requires at least one argument 

1. the first one required argument is the filename (in release directory)
2. the second one argument is the number of pages included in first part. if not defined this variable is set to 1633 