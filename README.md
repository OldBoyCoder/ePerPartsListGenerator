# ePerPartsListGenerator

Command line utility to generate a parts list PDF for vehicles covered by the ePer databases.

This code is used to read data for a particular model of car covered by Fiat's ePer database and
render it as a PDF file for viewing or printing.

The data is not available here, only the code.  The data is Copyright(c) Fiat Group Automobiles.

This code assumes does not require that ePer be installed on your computer, just that the install image
is accessible.  I coded it this way to avoid me having to swap installations in and out as I tested on 
different releases

In publishing this code I do so not so much as to provide the utility itself but more as a document of the structure of ePer so that future developers
can develop their own software.  

** Caveat **

Any information about the structure of the database is based on my analysis of the DB tables and examination
of the decompiled java code.  My car of interest is the Barchetta and so my assumptions have been tested against 
that car by comparing the pages I produce with the information displayed on ePer.  No guarantee is given
that I haven't missed something in the data.  Anyone finding problems with my analysis is welcome to contact me or 
change the code as they wish.

## Database information

The ePer system uses an MS-Access database to hold information about the parts drawings.  That database
is password protected.  Fortunately the password is in the Java code in method getConProps in IzmadoConnection.java so we can open it.
The password is the same in releases 20 and 84 so I suspect it has never changed.

I initially transferred the data from Access to SQL Server as I prefer the query tools for SQL Server, this is a 
simple task using SSMA.

In the current release of this project I read the data from the Access database directly as this reduces the steps
a user needs to go through to use this code.

## Releases

The database structure changes between releases.  I have release 20 and 84 and have documented their structure on the
following pages

[Release 20 database structure](Release20Structure.MD)

[Release 84 database structure](Release84Structure.MD)