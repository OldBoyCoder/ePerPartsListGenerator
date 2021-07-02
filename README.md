# ePerPartsListGenerator
Command line utility to generate a parts list PDF for vehicles covered by the ePer V84 database.

This code is used to read data for a particular model of car covered by Fiat's ePer database and
render it as a PDF file for viewing or printing.

The data is not available here, only the code.  The data is Copyright(c) 2011, Fiat Group Automobiles.

This code assumes that the data from the ePer system has been extracted and stored in a SQL Server database. 

## Hierarchy of a drawing

Consider drawing 55523/02 for the Nuovo Ducato (2J).  This drawing has one variant but 3 revisions

Fiat Professional » NUOVO DUCATO » 2J NUOVO DUCATO 2006 (2006-2014) » 555 APPARATUS AND ELECTRIC CONTROLS » 55523 CABLE HARNESS (FRONT) » 55523/02 CABLE ASSEMBLY (FRONT PART) 

|Grouping|Example|DB tables|Key|
|-|-|-|-|
|Make|Fiat Professional|MAKES|MK_COD
|Model|NUOVO DUCATO|COMM_MODGRP|CMG_COD
|Catalogue|2J NUOVO DUCATO 2006 (2006-2014)|CAT_COD
|Group|555 APPARATUS AND ELECTRIC CONTROLS|GROUPS, GROUPS_DSC|CAT_COD, GRP_COD
|Sub Group|55523 CABLE HARNESS (FRONT)|SUBGROUPS_BY_CAT, SUBGROUPS_DSC|CAT_COD, GRP_COD, SGRP_COD
|Drawing|55523/02 CABLE ASSEMBLY (FRONT PART)|DRAWINGS, TABLES_DSC|CAT_COD, GRP_COD, SGRP_COD, SGS_COD, VARIANTE, REVISIONE, TABLE_DSC_COD (links to DSC table)
|Drawing variant|Variant 1|
|Drawing variant revision|Revision 2|
|List of parts|.|TBDATA|