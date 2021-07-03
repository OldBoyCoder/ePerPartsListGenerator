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
|List of parts or a table|.|TBDATA|

## Table List

|Table|Key|Notes|
|-|-|-|
|APPLICABILITY|CAT_COD, GRP_COD, SGRP_COD, CDS_COD|Probably used as a quick way of finding which parts are used in which sub groups
|CARAT_DSC|CAT_COD, VMK_TYPE, LNG_COD|Decodes the first part of a variant.  Indiciates whether special version or drivers side as examples
|CAT_VAL|CAT_COD, VMK_TYPE, VMK_COD|Indicates which variants are available for a catalogue
|VMK_DSC|CAT_COD, VMK_TYPE, VMK_COD, LNG_COD|Desciptions of variants, seems to be both special editions and optional extras depending on the VMK_TYPE column
|CATALOGUES|MK_COD, CAT_COD|Lists all of the parts catalogues, a catalogue is a particular instance of a model.  So Model is Punto, Catalogue is a specific era or type of Punto.  Note, no language code on this table
|CLICHE|CLH_COD|A Cliche is a parts diagram for a part.  A typical exmple is a brake caliper. CPLX_PRT_COD is the link back to the PARTS table
|CODES_DSC|CDS_COD, LNG_COD|Holds the desciption for the part.  Maybe supplemented by data from DESC_AGG_DSC (which holds things like bolt size as 'M8', while CODES_DSC just has 'Bolt')
|CODES_REC|CDS_COD|Holds the part number for a reconditioned version of a part
|COLOURS_DSC|CAT_COD, COL_COD, LNG_COD|Holds descriptions for colours.  Seems to be a mix of paint colours and trim colours.  Prefix to COL_COD may indicate type of colour
|COLOURS_PATH|CAT_COD, COL_COD|Holds a path to an image illustrating the colour - not checked.
|COMM_MODGRP|MK2_COD, CMG_COD|The description for a model, again like CATALOGUES no language code
|CPXDATA|CLH_COD|Lists all of the parts for a Cliche
|DB_CONFIG|KEY|Presumably some meta data for the ePer application
|DESC_AGG_DSC|COD, LNG_COD|The second part of a parts description.  Not always there but extends CODES_DSC
|DRAWINGS|CAT_CD, GRP_COD, SGRP_COD, SGS_COD, VARIANTE, REVISIONE|A parts drawing, contains data about applicability of the drawing to modifications (MODIF) and versions (PATTERN)
|EXTERNAL_COLOURS_COMB||???
|EXTERNAL_COLOURS_DSC||???
|FAM_DSC|FAM_COD|Linked to from parts tables like PARTS, TBDATA and CPXDATA and gives a description of the family of a part
|GROUPS|CAT_COD, GRP_COD|A list of groups for a particular catalogue, a group has a 3 digit code such as 555 = Apparatus and Electric Controls
|GROUPS_DSC|GRP_COD, LNG_COD|Descriptions for groups|
|HS_FIGURINI|IMG_NAME|Looks to be some sort of hot spot table?|
|INTERNAL_COLOURS_COMB||Possibly defines what colour interior goes with other elements?|
|INTERNAL_COLOURS_DSC|CAT_COD, COD_COLORE_INT_VET, LNG_COD|Interior colour descriptions|
|INTERNAL_COMPONENTS_DSC|COMPC_D, LNG_COD|Only Italian entries
|KIT|CAT_COD, GRP_COD, SGRP_COD, SGS_COD, VARIANTE, REVISIONE, CPLX_PRT_COD, PRT_COD|A Kit of parts, example I've seen is brake pad sets, should get tagged on to end of parts list for a drawing or cliche
|KT_SCHEMA_INFO|PARAM_NAME|More database metadata
|LANG|LNG_COD|Contains the description of the various language codes
|MAKES|MK_COD|The names of the various Makes on the DB
|MAP_*||Used to draw the selectable hotspots on group and sub group drawings
|MARKET*||To do with the markets the cars were released in
|MDF_ACT|ACT_COD, MDF_COD, CAT_COD|Contains the attributes of a vehicle modification
|MODIF_DSC|CAT_COD, MDF_COD, LNGCOD|Conatins the descriptions of vehicle modifications
|MSG_PART_BY_MKT|MKT_COD|Table empty so difficult to see what it is
|MVS|CAT_COD, MOD_COD ...|Looks like a list of models - more work needed
|NOTES_DSC|NTS_COD, LNG_COD|A note about a part in a parts table (linked from, at least, TBDATA)
|PARTS|CDS_COD or PRT_COD|The data about a specific part - non vehicle specific, linked to a car via the TBDATA and CPXDATA tables
|PROMO*|PROMO_ID|Holds details about promotions, don't care about these
|REC_PARTS||A list of what the reconditioned and wrecked part numbers are for a given part
|RPLNT*||Something to do with replacements|
|SPECIAL_KIT||Empty table
|SUBGROUPS_BY_CAT|CAT_COD, GRP_COD|List of all groups and subgroups for a catalogue
|SUBGROUPS_DSC|LNG_COD, GRP_COD, SGRP_COD|Descriptions for sub groups
|TABLES_DSC|COD, LNG_COD|Description of a table which is sort of a drawing
|TBDATA|CAT_COD, GRP_COD, SGRP_COD, SGS_COD, VARIANTE, REVISIONE|The list of parts for a table/drawing
|TRANCHE|CAT_COD|Looks like ranges of VINs for a catalogue, Barchetta is only one entry
|UN_OF_MEAS|UM_COD|Descriptions of units of measurement|
|VIN|VIN_COD|Not sure
|VMK_DSC|CAT_COD, VMK_TYPE, VMK_COD, LNG_COD|Description of variants (special editions etc)







