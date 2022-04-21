# ePerPartsListGenerator

Command line utility to generate a parts list PDF for vehicles covered by the ePer databases.

This code is used to read data for a particular model of car covered by Fiat's ePer database and
render it as a PDF file for viewing or printin or as a tab delimited file for loading into a spreadsheet program.

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

## Understanding the ePer hierarchy

Across the two releases I have examined the following hierarchy is evident when looking at the data

The `MAKES` table lists all the car makes covered by the database.  Using column `MK_COD` from this table allows one to find:

The `CATALOGUES` table lists all the catalogues of parts in the database.  Column `CAT_COD` can then be used to find:

The `GROUPS` table which lists the major grouping of parts in the database.  The groups are identified by a three digit number which
can be seen in ePer as the the column `GRP_COD`

A `SUBGROUPS_DSC` table which lists the sub-groups for a group.  The subgroup is identified by the column `SGRP_COD` from where you can get to the drawings for the sub group

The `DRAWINGS` table has a compound key of `CAT_COD`, `GRP_COD`, `SGRP_COD`, `SGS_COD` and `DRW_NUM`.  `SGS_COD` is a 
further breakdown of subgroup, it is typically used for when a drawing is much changed between revisions of the car
`DRW_NUM` identifies the number of drawing.

Consider the code 10222/01
- 102 - The group code indicating 'Fuel end exhaust systems'
- 22 - The sub-group code indicating 'Accelerator control linkage'
- 00 - The sub-sub-group code indicating the sub group for the 'M1' engine

Within a drawing there will be a list of parts.  For some parts there is a further diagram breaking down that part further.
Thes efurther breakdowns are known as `CLICHES` in the database.  A cliche may well be shared between different vehicles
and so is known by the part number it expands.

As well as parts a drawing may also include `KIT`s these are where a set of parts are available to buy together.
You will see this most often for brake service kits.

## Releases

The database structure changes between releases.  I have release 20 and 84 and have documented their structure on the
following pages

[Release 20 database structure](Release20Structure.MD)

[Release 84 database structure](Release84Structure.MD)