//PAGNAHGA JOB (100019,AGNIS),260SJGR,CLASS=C,REGION=6M,
//             MSGCLASS=T,TIME=1000,USER=PROD
//*MAIN LINES=(150,W)
//*FORMAT PR,DD=JESJCL
//*FORMAT PR,DD=JESMSGL
//*FORMAT PR,DD=JESYSMSG
//*FORMAT PR,DD=SRTMSG
//*FORMAT PR,DD=SYSPRINT
//*FORMAT PR,DD=JESJCL,DEST=AGNIS
//*FORMAT PR,DD=JESMSGLG,DEST=AGNIS
//*FORMAT PR,DD=JESYSMSG,DEST=AGNIS
//*FORMAT PR,DD=SRTMSG,DEST=AGNIS
//*FORMAT PR,DD=SYSPRINT,DEST=AGNIS
//*FORMAT PR,DD=JESMSGLG,AB=PRT,DEST=BOMB
//*FORMAT PR,DD=JESMSGLG,DEST=TRAK
//*FORMAT PR,DD=CA07RMS.RMS@2X.RMSRPT,DEST=OPER
//*FORMAT PR,DD=REPORTF,DEST=AGNIS
//*FORMAT PR,DD=SEEDLOG,DEST=AGNIS
//*FORMAT PR,DD=JOBRPT,DEST=AGNIS
//*FORMAT PR,DD=FOUTALL,DEST=AGNIS
//*FORMAT PR,DD=JOBLOG,DEST=AGNIS
//*FORMAT PR,DD=CBEWS,DEST=AGNIS
//*FORMAT PR,DD=LTDATA,DEST=AGNIS
//*FORMAT PR,DD=LLKDB,DEST=AGNIS
//*FORMAT PR,DD=LLKSUD,DEST=AGNIS
//*FORMAT PR,DD=LACSLOG,DEST=AGNIS
//*FORMAT PR,DD=DPVDB,DEST=AGNIS
//*FORMAT PR,DD=DPVSDB,DEST=AGNIS
//*FORMAT PR,DD=DPVHDB,DEST=AGNIS
//*FORMAT PR,DD=SLKDB,DEST=AGNIS
//*FORMAT PR,DD=SYSPRT1,DEST=AGNIS
//*FORMAT PR,DD=ABNLIGNR,DEST=AGNIS
//*FORMAT PR,DD=CEEDUMP,DEST=AGNIS
//*FORMAT PR,DD=PBBATCH,DEST=AGNIS
//*FORMAT PR,DD=SYSPRT7,DEST=AGNIS
//*FORMAT PR,DD=PBREPORT,DEST=AGNIS
//* @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
//* @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
/*
//DOWNLD   EXEC PGM=IDCAMS
//INPUT    DD  DSN=PRD.REIX300.BS.AHDEPCOV,DISP=SHR
//OUTPUT   DD  DSN=PRD.REIX300.SC.DEPCOVFL,DISP=(,CATLG,DELETE),
//  UNIT=SYSDA,DCB=(LRECL=200,RECFM=FB),
//  SPACE=(TRK,430,RLSE),LABEL=RETPD=20
//SYSPRINT DD  SYSOUT=*
//SYSIN    DD *
  REPRO INFILE(INPUT),OUTFILE(OUTPUT)
/*
//STRUN4   EXEC PGM=AGNAHGA,TIME=1000
//STEPLIB  DD  DSN=SYS2.PRODA,DISP=SHR
//* //         DD  DSN=SYS2.SYSTEST,DISP=SHR
//* //PREMAST  DD  DSN=ORD.P56300.AGNMST10.G6244V00,DISP=SHR
//* //         DD  DSN=ORD.P56300.AGNMST48.G6243V00,DISP=SHR
//PREMAST  DD  DSN=ORD.P56300.AGNMST10(-0),DISP=SHR
//         DD  DSN=ORD.P56300.AGNMST48(-0),DISP=SHR
//DEPFILE  DD  DSN=PRD.REIX300.SC.DEPCOVFL,DISP=SHR
//OUTFILE  DD  DSN=PRD.AGNAHGA.SC.RAW.DATA,
//  DISP=(,CATLG,DELETE),
//  UNIT=SYSDA,SPACE=(TRK,(1500,100),RLSE),
//  LABEL=RETPD=60
//SYSUDUMP DD  SYSOUT=*
//SYSOUT   DD  SYSOUT=*
/*
//* SORT ON ZIP-CODE TO IMPROVE PERFORMANCE OF FINALIST
//*
//SORTZIP1 EXEC  SORTD,CYL=100
//SYSOUT   DD  SYSOUT=*
//SORTIN   DD  DSN=PRD.AGNAHGA.SC.RAW.DATA,DISP=SHR
//SORTOUT  DD  DSN=PRD.AGNAHGA.SC.PRESORT,
//  DISP=(,CATLG,DELETE),
//  UNIT=SYSDA,SPACE=(TRK,(1500,100),RLSE),
//  LABEL=RETPD=90
//SYSIN    DD  *
 SORT FIELDS=(173,5,BI,A),EQUALS
 END
/*
//*------------------------------------------------------------------
//*  ASSIGN ZIP+4  (VIA FINALIST)
//*
//******************************************************
//FINALIST EXEC PGM=FINALIST,TIME=1000
//STEPLIB  DD  DISP=SHR,DSN=ACSNS.FINALIST.LOADLIB
//*
//******** FULL-VOLUME PRODUCTION INPUT FILE
//SYSUT1   DD  DSN=PRD.AGNAHGA.SC.PRESORT,DISP=SHR
//*
//*********** DRIVER FILES *******************
//SEEDLOG  DD  SYSOUT=*
//*
//* ------ JOB FILE -----------------------------------------
//PBFNJOB  DD  DSN=PRD.PAGNAHGA.SC.FINALIST.JOB,DISP=SHR
//*
//*------- DEF FILE -----------------------------------------
//PBFNDEF  DD  DSN=PRD.PAGNAHGA.SC.FINALIST.DEF,DISP=SHR
//*
//*------- CFG FILE -------------------
//PBFNCFG  DD  DSN=SYS2.JCLDATA(FINALIST),DISP=SHR
//*
//*
//******** OUTPUT WITH ALL INPUT (GOOD AND BAD ADDRESSES)
//SYSUT2   DD  DSN=PRD.AGNAHGA.SC.ZIP4,
//  DISP=(,CATLG,DELETE),
//  UNIT=SYSDA,SPACE=(TRK,(1500,100),RLSE),
//  DCB=(RECFM=FB,LRECL=220),
//  LABEL=RETPD=90
//*
//******** OUTPUT WITH COMPLETE ZIP+4 (GOOD ADDRESS IS IMPLIED)
//VALIDF   DD  DUMMY,
//  DCB=LRECL=220
//*
//******** OUTPUT WITH INCOMPLETE ZIP+4  (BAD ADDRESS IS IMPLIED)
//ERRORF   DD  DUMMY,
//  DCB=LRECL=220
//*
//REPORTF  DD  SYSOUT=*,
//  DCB=(LRECL=57,RECFM=FB)
//*
//*
//******** JOB REPORT INFORMATION
//JOBRPT   DD  SYSOUT=*
//*
//FOUTALL  DD  SYSOUT=*
//*
//******** DRIVER LOG INFORMATION
//JOBLOG   DD  SYSOUT=*
//*
//*           DATABASE FILE
//CBDATA$  DD  DSN=VONB.ORD.FINALIST.DATAFILE,DISP=SHR
//CBDATA DD SUBSYS=(BLSR,'DDNAME=CBDATA$,BUFND=100,RMODE31=ALL,MSG=I')
//*
//*           CITY/STATE FILE
//CBY1     DD  DSN=VONS.ORD.FINALIST.CITYVSAM,DISP=SHR
//CBCTYST DD SUBSYS=(BLSR,'DDNAME=CBY1,BUFND=2600,RMODE31=ALL,MSG=I')
//*
//* EWS FILE
//CBEWS    DD  DUMMY
//*
//* ELOT FILE
//LTDATA   DD  DUMMY
//*
//* LACS FILES
//LLKDB    DD  DUMMY
//LLKSUD   DD  DUMMY
//LACSLOG  DD  SYSOUT=*
//*
//* DPV FILES
//DPVDB    DD  DUMMY
//DPVSDB   DD  DUMMY
//DPVHDB   DD  DUMMY
//DPVSUD   DD  DISP=SHR,DSN=ACSNS.FINALIST.DPVSUD
//*
//* SUITELINK FILES
//SLKDB    DD  DUMMY
//*
//SYSPRT1  DD  SYSOUT=*
//SYSOUT   DD  SYSOUT=*,DCB=LRECL=137
//SYSUDUMP DD  DUMMY
//*
//ABNLIGNR DD  DUMMY
//CEEDUMP  DD  SYSOUT=*
//PBBATCH  DD  SYSOUT=*
//SYSPRT7  DD  SYSOUT=*
//PBREPORT DD  SYSOUT=*
//SYSPRINT DD  SYSOUT=*
/*
/*
//*
//SORTZIP4 EXEC  SORTD,CYL=100
//SYSOUT   DD  SYSOUT=*
//SORTIN   DD  DSN=PRD.AGNAHGA.SC.ZIP4,DISP=SHR
//SORTOUT  DD  DSN=PRD.AGNAHGA.SC.SORTZIP4,
//  DISP=(,CATLG,DELETE),LABEL=RETPD=90,
//  DCB=(LRECL=220,RECFM=FB),
//  UNIT=SYSDA,SPACE=(TRK,(1500,100),RLSE)
//SYSIN    DD  *
 SORT FIELDS=(173,9,BI,A),EQUALS
 END
/*
//**    SEPARATE GOOD AND BAD ADDRESS RECORDS
//SEPARATE EXEC EZTPCG
//IN01     DD  DSN=PRD.AGNAHGA.SC.SORTZIP4,DISP=SHR
//GOODADDR DD  DSN=PRD.AGNAHGA.SC.GOODADDR,
//  UNIT=SYSDA,DCB=(LRECL=220,RECFM=FB),SPACE=(TRK,(1500,100),RLSE),
//  LABEL=RETPD=90,DISP=(,CATLG,DELETE)
//*   TO: GOOD-ZIP+4
//*
//BADADDR  DD  DSN=PRD.AGNAHGA.SC.BADADDR,
//  UNIT=SYSDA,DCB=(LRECL=220,RECFM=FB),SPACE=(TRK,(1500,1),RLSE),
//  LABEL=RETPD=90,DISP=(,CATLG,DELETE)
//*   TO: ADDRESS-BAD
//*
//*  THESE LAYOUTS MAY NOT BE CORRECT TOWARD THE END
//*  BUT THEY DO HAVE THE CORRECT LRECL!
//*
//SYSPRINT DD  SYSOUT=*
//SYSIN    DD  DSN=SYS2.JCLDATA(PAGN0435),DISP=SHR
//