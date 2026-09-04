/*
================================================================================
MIGRACION / ACTUALIZACION BD CLIENTE - CERES
================================================================================
Origen: 2.0.- SCRIPT_BD_2026.08.29.19.25.sql

OBJETIVO
  1. Crear, si no existen: dbo.COMPANY, dbo.QTK_OBJETO_POLITICA,
     dbo.RankAtt001.
  2. Cargar la data existente en el script origen sin duplicarla al repetir
     la ejecucion.
  3. Ajustar CHECKINOUT.WorkCode a varchar(24) NULL.
  4. Agregar/ajustar USERINFO.Pin1 a int NULL.
  5. Informar mediante PRINT si los objetos/registros ya existian o fueron
     creados/modificados.

SEGURIDAD
  - No ejecuta DROP, TRUNCATE ni DELETE.
  - No elimina ni reemplaza datos existentes.
  - No ejecuta SQL contra otra base: solo modifica la base donde se ejecute.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ============================================================================
   1. COMPANY
   ============================================================================ */
IF OBJECT_ID(N'[dbo].[COMPANY]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[COMPANY](
	[COMPANYID]  [int] IDENTITY(1,1) NOT NULL,
	[SCIA_TAXID] [varchar](15) NOT NULL,
	[SCIA_DESCRIP] [varchar](100) NOT NULL,
	[SCIA_TELF] [varchar](15) NULL,
	[SCIA_MOVIL] [varchar](15) NULL,
	[SCIA_EMAIL] [varchar](30) NULL,
	[SCIA_DIRECC] [varchar](150) NULL,
	[DEPTID] [int] NOT NULL,
	[estado_row] [char](1) NOT NULL,
 CONSTRAINT [COMPANYID] PRIMARY KEY CLUSTERED 
(
	[COMPANYID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] 

    PRINT 'OK: Tabla [dbo].[COMPANY] creada.';
END
ELSE
BEGIN
    PRINT 'AVISO: Tabla [dbo].[COMPANY] ya existe; no se creo nuevamente.';
END
GO

/* Asegurar el indice unico de RUC */
IF OBJECT_ID(N'[dbo].[COMPANY]', N'U') IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[COMPANY]')
      AND name = N'SCIA_TAXID'
)
BEGIN
CREATE UNIQUE NONCLUSTERED INDEX [SCIA_TAXID] ON [dbo].[COMPANY] 
(
	[SCIA_TAXID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
    PRINT 'OK: Indice unico [SCIA_TAXID] creado en [dbo].[COMPANY].';
END
ELSE IF OBJECT_ID(N'[dbo].[COMPANY]', N'U') IS NOT NULL
BEGIN
    PRINT 'AVISO: Indice [SCIA_TAXID] de [dbo].[COMPANY] ya existe.';
END
GO

/* Data COMPANY: el script origen contiene COMPANYID=1 */
IF NOT EXISTS (SELECT 1 FROM [dbo].[COMPANY] WHERE [COMPANYID] = 1)
BEGIN
	SET IDENTITY_INSERT [dbo].[COMPANY] ON
    INSERT INTO [dbo].[COMPANY]
           ([COMPANYID],[SCIA_TAXID],[SCIA_DESCRIP],[SCIA_TELF],[SCIA_MOVIL],
            [SCIA_EMAIL],[SCIA_DIRECC],[DEPTID],[estado_row])
    VALUES (1,'20512639870','ASOCIACION DE PROPIETARIOS MEGA PLAZA CERES',
            NULL,NULL,NULL,NULL,1,'A');
	SET IDENTITY_INSERT [dbo].[COMPANY] OFF

    PRINT 'OK: Registro COMPANYID=1 agregado a [dbo].[COMPANY].';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro COMPANYID=1 ya existe; no se inserto.';
END
GO

/* ============================================================================
   2. QTK_OBJETO_POLITICA
   ============================================================================ */
IF OBJECT_ID(N'[dbo].[QTK_OBJETO_POLITICA]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[QTK_OBJETO_POLITICA](
	[IdOBJPOL] [int] NOT NULL,
	[sCodOBJPOL] [varchar](6) NOT NULL,
	[sNombre] [varchar](50) NOT NULL,
	[sDescAbrev] [varchar](20) NULL,
	[TIPO_OBJETO] [varchar](3) NOT NULL,
	[nOrden] [int] NULL,
	[nGrupo] [int] NULL,
	[nSecuencia] [int] NULL,
	[PROJECT] [varchar](6) NULL,
	[TIPO_PROJECT] [varchar](3) NULL,
	[COD_PROC] [varchar](10) NULL,
	[TIPO_PROC] [varchar](3) NULL,
	[POLITICA] [varchar](30) NULL,
	[TIPO_POLITICA] [varchar](3) NULL,
	[CODZON] [varchar](5) NULL,
	[IdOBJPOL_PADRE] [int] NULL,
	[OBJPOL_titulo] [varchar](50) NULL,
	[OBJPOL_observ] [varchar](75) NULL,
	[OBJPOL_tipodato] [varchar](1) NULL,
	[OBJPOL_lencampo] [int] NULL,
	[OBJPOL_ordcampo] [int] NULL,
	[OBJPOL_tipoctrl] [varchar](15) NULL,
	[OBJPOL_anchoctrl] [int] NULL,
	[OBJPOL_altoctrl] [int] NULL,
	[OBJPOL_pwdchar] [varchar](1) NULL,
	[OBJPOL_ddstyle] [varchar](1) NULL,
	[OBJPOL_dddisplaym] [varchar](20) NULL,
	[OBJPOL_ddvaluem] [varchar](20) NULL,
	[OBJPOL_dddatasource] [varchar](max) NULL,
	[OBJPOL_maxvalores] [int] NULL,
	[OBJPOL_cpofuncion] [varchar](255) NULL,
	[OBJPOL_nValor01] [float] NULL,
	[OBJPOL_nValor02] [float] NULL,
	[OBJPOL_nValor03] [float] NULL,
	[OBJPOL_nValor04] [float] NULL,
	[OBJPOL_nValor05] [float] NULL,
	[OBJPOL_nValor06] [float] NULL,
	[OBJPOL_nValor07] [float] NULL,
	[OBJPOL_nValor08] [float] NULL,
	[OBJPOL_nValor09] [float] NULL,
	[OBJPOL_nValor10] [float] NULL,
	[OBJPOL_nValor11] [float] NULL,
	[OBJPOL_nValor12] [float] NULL,
	[OBJPOL_nValor13] [float] NULL,
	[OBJPOL_nValor14] [float] NULL,
	[OBJPOL_nValor15] [float] NULL,
	[OBJPOL_nValor16] [float] NULL,
	[OBJPOL_nValor17] [float] NULL,
	[OBJPOL_nValor18] [float] NULL,
	[OBJPOL_nValor19] [float] NULL,
	[OBJPOL_nValor20] [float] NULL,
	[OBJPOL_sValor01] [varchar](50) NULL,
	[OBJPOL_sValor02] [varchar](50) NULL,
	[OBJPOL_sValor03] [varchar](50) NULL,
	[OBJPOL_sValor04] [varchar](50) NULL,
	[OBJPOL_sValor05] [varchar](50) NULL,
	[OBJPOL_sValor06] [varchar](50) NULL,
	[OBJPOL_sValor07] [varchar](50) NULL,
	[OBJPOL_sValor08] [varchar](50) NULL,
	[OBJPOL_sValor09] [varchar](50) NULL,
	[OBJPOL_sValor10] [varchar](50) NULL,
	[OBJPOL_sValor11] [varchar](50) NULL,
	[OBJPOL_sValor12] [varchar](50) NULL,
	[OBJPOL_sValor13] [varchar](50) NULL,
	[OBJPOL_sValor14] [varchar](50) NULL,
	[OBJPOL_sValor15] [varchar](50) NULL,
	[OBJPOL_sValor16] [varchar](50) NULL,
	[OBJPOL_sValor17] [varchar](50) NULL,
	[OBJPOL_sValor18] [varchar](50) NULL,
	[OBJPOL_sValor19] [varchar](50) NULL,
	[OBJPOL_sValor20] [varchar](50) NULL,
	[OBJPOL_sValor21] [varchar](50) NULL,
	[OBJPOL_sValor22] [varchar](50) NULL,
	[OBJPOL_sValor23] [varchar](50) NULL,
	[OBJPOL_sValor24] [varchar](50) NULL,
	[OBJPOL_sValor25] [varchar](50) NULL,
	[OBJPOL_sValor26] [varchar](50) NULL,
	[OBJPOL_sValor27] [varchar](50) NULL,
	[OBJPOL_sValor28] [varchar](50) NULL,
	[OBJPOL_sValor29] [varchar](50) NULL,
	[OBJPOL_sValor30] [varchar](50) NULL,
	[OBJPOL_01] [varchar](max) NULL,
	[OBJPOL_02] [varchar](max) NULL,
	[OBJPOL_03] [varchar](max) NULL,
	[OBJPOL_04] [varchar](max) NULL,
	[OBJPOL_05] [varchar](max) NULL,
	[OBJPOL_06] [varchar](max) NULL,
	[OBJPOL_07] [varchar](max) NULL,
	[OBJPOL_08] [varchar](max) NULL,
	[OBJPOL_09] [varchar](max) NULL,
	[OBJPOL_10] [varchar](max) NULL,
	[OBJPOL_11] [varchar](max) NULL,
	[OBJPOL_12] [varchar](max) NULL,
	[OBJPOL_13] [varchar](max) NULL,
	[OBJPOL_14] [varchar](max) NULL,
	[OBJPOL_15] [varchar](max) NULL,
	[OBJPOL_16] [varchar](max) NULL,
	[OBJPOL_17] [varchar](max) NULL,
	[OBJPOL_18] [varchar](max) NULL,
	[OBJPOL_19] [varchar](max) NULL,
	[OBJPOL_20] [varchar](max) NULL,
	[OBJPOL_21] [varchar](max) NULL,
	[OBJPOL_22] [varchar](max) NULL,
	[OBJPOL_23] [varchar](max) NULL,
	[OBJPOL_24] [varchar](max) NULL,
	[OBJPOL_25] [varchar](max) NULL,
	[OBJPOL_26] [varchar](max) NULL,
	[OBJPOL_27] [varchar](max) NULL,
	[OBJPOL_28] [varchar](max) NULL,
	[OBJPOL_29] [varchar](max) NULL,
	[OBJPOL_30] [varchar](max) NULL,
	[CODORG] [varchar](2) NULL,
	[IdOrganizacion] [int] NULL,
	[estado_row] [varchar](1) NOT NULL,
	[fechahora_ins] [datetime] NOT NULL,
	[fechahora_upd] [datetime] NULL,
	[fechahora_del] [datetime] NULL,
	[coduser_ins] [varchar](20) NOT NULL,
	[coduser_upd] [varchar](20) NULL,
	[coduser_del] [varchar](20) NULL,
	[OBJPOL_flag01] [varchar](6) NULL,
	[OBJPOL_flag02] [varchar](6) NULL,
	[OBJPOL_flag03] [varchar](6) NULL,
	[OBJPOL_flag04] [varchar](6) NULL,
	[OBJPOL_flag05] [varchar](6) NULL,
	[OBJPOL_flag06] [varchar](6) NULL,
	[OBJPOL_flag07] [varchar](6) NULL,
	[OBJPOL_flag08] [varchar](6) NULL,
	[OBJPOL_flag09] [varchar](6) NULL,
	[OBJPOL_flag10] [varchar](6) NULL,
 CONSTRAINT [PK__QTK_OBJE__CA150CC11EC0AA92] PRIMARY KEY CLUSTERED 
(
	[IdOBJPOL] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

    PRINT 'OK: Tabla [dbo].[QTK_OBJETO_POLITICA] creada.';
END
ELSE
BEGIN
    PRINT 'AVISO: Tabla [dbo].[QTK_OBJETO_POLITICA] ya existe; no se creo nuevamente.';
END
GO

/* Data QTK_OBJETO_POLITICA: se cargan los registros del script origen.
   La clave usada para evitar duplicados es IdOBJPOL. */
IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 0)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (0, N'000', N'ATTMNG488PARM00', N'', N'PAR', 0, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'RUC', N'', N' ', 11, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', 218930534, 25875016, 580304501, 91380703, 705228804, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'153145', N'171020', N'200798', N'167504', N'103999', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=0.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=0; no se inserto.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 1)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (1, N'001', N'ATTMNG488GRPRPTBASIC', N'', N'GRT', 1, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'', N'', N' ', NULL, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=1.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=1; no se inserto.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 2)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (2, N'002', N'ATTMNG488GRPRPTRESUM', N'', N'GRT', 2, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'', N'', N' ', NULL, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=2.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=2; no se inserto.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 3)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (3, N'003', N'ATTMNG488GRPRPTPRENOMINA', N'', N'GRT', 3, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'', N'', N' ', NULL, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=3.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=3; no se inserto.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 4)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (4, N'004', N'ATTMNG488GRPRPTHEPER', N'', N'GRT', 4, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'', N'', N' ', NULL, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=4.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=4; no se inserto.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 5)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (5, N'005', N'ATTMNG488GRPRPTCUSTOM', N'', N'GRT', 5, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'', N'', N' ', NULL, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=5.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=5; no se inserto.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 6)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (6, N'006', N'ATTMNG488GRPRPTBASICCRFR', N'', N'GRT', 6, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'', N'', N' ', NULL, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=6.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=6; no se inserto.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 7)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (7, N'007', N'ATTMNG488GRPRPTRESUMCRFR', N'', N'GRT', 7, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'', N'', N' ', NULL, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=7.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=7; no se inserto.';
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[QTK_OBJETO_POLITICA] WHERE [IdOBJPOL] = 8)
BEGIN
    INSERT [dbo].[QTK_OBJETO_POLITICA] ([IdOBJPOL], [sCodOBJPOL], [sNombre], [sDescAbrev], [TIPO_OBJETO], [nOrden], [nGrupo], [nSecuencia], [PROJECT], [TIPO_PROJECT], [COD_PROC], [TIPO_PROC], [POLITICA], [TIPO_POLITICA], [CODZON], [IdOBJPOL_PADRE], [OBJPOL_titulo], [OBJPOL_observ], [OBJPOL_tipodato], [OBJPOL_lencampo], [OBJPOL_ordcampo], [OBJPOL_tipoctrl], [OBJPOL_anchoctrl], [OBJPOL_altoctrl], [OBJPOL_pwdchar], [OBJPOL_ddstyle], [OBJPOL_dddisplaym], [OBJPOL_ddvaluem], [OBJPOL_dddatasource], [OBJPOL_maxvalores], [OBJPOL_cpofuncion], [OBJPOL_nValor01], [OBJPOL_nValor02], [OBJPOL_nValor03], [OBJPOL_nValor04], [OBJPOL_nValor05], [OBJPOL_nValor06], [OBJPOL_nValor07], [OBJPOL_nValor08], [OBJPOL_nValor09], [OBJPOL_nValor10], [OBJPOL_nValor11], [OBJPOL_nValor12], [OBJPOL_nValor13], [OBJPOL_nValor14], [OBJPOL_nValor15], [OBJPOL_nValor16], [OBJPOL_nValor17], [OBJPOL_nValor18], [OBJPOL_nValor19], [OBJPOL_nValor20], [OBJPOL_sValor01], [OBJPOL_sValor02], [OBJPOL_sValor03], [OBJPOL_sValor04], [OBJPOL_sValor05], [OBJPOL_sValor06], [OBJPOL_sValor07], [OBJPOL_sValor08], [OBJPOL_sValor09], [OBJPOL_sValor10], [OBJPOL_sValor11], [OBJPOL_sValor12], [OBJPOL_sValor13], [OBJPOL_sValor14], [OBJPOL_sValor15], [OBJPOL_sValor16], [OBJPOL_sValor17], [OBJPOL_sValor18], [OBJPOL_sValor19], [OBJPOL_sValor20], [OBJPOL_sValor21], [OBJPOL_sValor22], [OBJPOL_sValor23], [OBJPOL_sValor24], [OBJPOL_sValor25], [OBJPOL_sValor26], [OBJPOL_sValor27], [OBJPOL_sValor28], [OBJPOL_sValor29], [OBJPOL_sValor30], [OBJPOL_01], [OBJPOL_02], [OBJPOL_03], [OBJPOL_04], [OBJPOL_05], [OBJPOL_06], [OBJPOL_07], [OBJPOL_08], [OBJPOL_09], [OBJPOL_10], [OBJPOL_11], [OBJPOL_12], [OBJPOL_13], [OBJPOL_14], [OBJPOL_15], [OBJPOL_16], [OBJPOL_17], [OBJPOL_18], [OBJPOL_19], [OBJPOL_20], [OBJPOL_21], [OBJPOL_22], [OBJPOL_23], [OBJPOL_24], [OBJPOL_25], [OBJPOL_26], [OBJPOL_27], [OBJPOL_28], [OBJPOL_29], [OBJPOL_30], [CODORG], [IdOrganizacion], [estado_row], [fechahora_ins], [fechahora_upd], [fechahora_del], [coduser_ins], [coduser_upd], [coduser_del], [OBJPOL_flag01], [OBJPOL_flag02], [OBJPOL_flag03], [OBJPOL_flag04], [OBJPOL_flag05], [OBJPOL_flag06], [OBJPOL_flag07], [OBJPOL_flag08], [OBJPOL_flag09], [OBJPOL_flag10]) VALUES (8, N'008', N'ATTMNG488GRPRPTCUSTOMCRFR', N'', N'GRT', 8, NULL, NULL, N'', N'   ', N'', N'   ', N'', N'   ', N'', NULL, N'', N'', N' ', NULL, NULL, N'', NULL, NULL, N' ', N' ', N'', N'', N'', NULL, N'', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, N'i>E?CF?HIEHGLE@BCP?C', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'  ', NULL, N'A', CAST(0x0000AED600000000 AS DateTime), NULL, NULL, N'MASTER', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'', N'')
    PRINT 'OK: Registro agregado a [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=8.';
END
ELSE
BEGIN
    PRINT 'AVISO: Registro ya existe en [dbo].[QTK_OBJETO_POLITICA] [IdOBJPOL]=8; no se inserto.';
END
GO

/* ============================================================================
   3. RankAtt001
   ============================================================================ */
IF OBJECT_ID(N'[dbo].[RankAtt001]', N'U') IS NULL
BEGIN
CREATE TABLE [dbo].[RankAtt001](
	[UserID] [bigint] NULL,
	[Badgenumber] [nvarchar](24) NULL,
	[Name] [nvarchar](40) NULL,
	[SSN] [nvarchar](20) NULL,
	[Orden] [nvarchar](5) NULL,
	[PuntualTempra] [nvarchar](2) NULL,
	[Falta] [nvarchar](2) NULL,
	[Tardanza] [nvarchar](2) NULL,
	[FechaAsi] [nvarchar](10) NULL,
	[HoraAsi] [nvarchar](8) NULL
) ON [PRIMARY]

    PRINT 'OK: Tabla [dbo].[RankAtt001] creada.';
END
ELSE
BEGIN
    PRINT 'AVISO: Tabla [dbo].[RankAtt001] ya existe; no se creo nuevamente.';
END
GO

PRINT 'INFO: El script origen no contiene INSERT de datos para [dbo].[RankAtt001].';
GO

/* ============================================================================
   4. CHECKINOUT.WorkCode
   Objetivo: [varchar](24) NULL
   ============================================================================ */
IF OBJECT_ID(N'[dbo].[CHECKINOUT]', N'U') IS NULL
BEGIN
    PRINT 'ERROR: No existe [dbo].[CHECKINOUT]. No se pudo ajustar WorkCode.';
END
ELSE IF COL_LENGTH(N'dbo.CHECKINOUT', N'WorkCode') IS NULL
BEGIN
    ALTER TABLE [dbo].[CHECKINOUT]
        ADD [WorkCode] [varchar](24) NULL;

    PRINT 'OK: Se agrego [dbo].[CHECKINOUT].[WorkCode] como varchar(24) NULL.';
END
ELSE
BEGIN
    DECLARE @WorkCodeIsCorrect bit = 0;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID(N'[dbo].[CHECKINOUT]')
          AND c.name = N'WorkCode'
          AND t.name = N'varchar'
          AND c.max_length = 24
          AND c.is_nullable = 1
    )
        SET @WorkCodeIsCorrect = 1;

    IF @WorkCodeIsCorrect = 1
    BEGIN
        PRINT 'AVISO: [dbo].[CHECKINOUT].[WorkCode] ya es varchar(24) NULL.';
    END
    ELSE
    BEGIN
        --ALTER TABLE [dbo].[CHECKINOUT] DROP  CONSTRAINT DF__CHECKINOU__WorkC__19AACF41

        ALTER TABLE [dbo].[CHECKINOUT]
            ALTER COLUMN [WorkCode] [varchar](24) NULL;

        PRINT 'OK: [dbo].[CHECKINOUT].[WorkCode] ajustado a varchar(24) NULL.';
    END
END
GO

/* ============================================================================
   5. USERINFO.Pin1
   Objetivo: [int] NULL
   ============================================================================ */
IF OBJECT_ID(N'[dbo].[USERINFO]', N'U') IS NULL
BEGIN
    PRINT 'ERROR: No existe [dbo].[USERINFO]. No se pudo ajustar Pin1.';
END
ELSE IF COL_LENGTH(N'dbo.USERINFO', N'Pin1') IS NULL
BEGIN
    ALTER TABLE [dbo].[USERINFO]
        ADD [Pin1] [int] NULL;

    PRINT 'OK: Se agrego [dbo].[USERINFO].[Pin1] como int NULL.';
END
ELSE
BEGIN
    DECLARE @Pin1IsCorrect bit = 0;

    IF EXISTS (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID(N'[dbo].[USERINFO]')
          AND c.name = N'Pin1'
          AND t.name = N'int'
          AND c.is_nullable = 1
    )
        SET @Pin1IsCorrect = 1;

    IF @Pin1IsCorrect = 1
    BEGIN
        PRINT 'AVISO: [dbo].[USERINFO].[Pin1] ya es int NULL.';
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[USERINFO]
            ALTER COLUMN [Pin1] [int] NULL;

        PRINT 'OK: [dbo].[USERINFO].[Pin1] ajustado a int NULL.';
    END
END
GO

PRINT '===============================================================';
PRINT 'PROCESO FINALIZADO CORRECTAMENTE.';
PRINT 'El script puede ejecutarse nuevamente sin duplicar los registros';
PRINT 'protegidos por sus claves de control.';
PRINT '===============================================================';
GO
