SET NOCOUNT ON;

IF OBJECT_ID(N'[19.10_pacienty]',N'U') IS NULL
CREATE TABLE [19.10_pacienty](
 id_pacienta INT IDENTITY PRIMARY KEY, fio NVARCHAR(150) NOT NULL, pol NVARCHAR(20) NOT NULL,
 data_rozhdeniya DATE NOT NULL, seriya_pasporta NVARCHAR(20), nomer_pasporta NVARCHAR(20),
 polis_oms NVARCHAR(30) NOT NULL UNIQUE, adres NVARCHAR(250), telefon NVARCHAR(30), foto VARBINARY(MAX));
ELSE IF COL_LENGTH('19.10_pacienty','foto') IS NULL ALTER TABLE [19.10_pacienty] ADD foto VARBINARY(MAX);

IF OBJECT_ID(N'[19.10_vrachi]',N'U') IS NULL
CREATE TABLE [19.10_vrachi](id_vracha INT IDENTITY PRIMARY KEY,fio NVARCHAR(150) NOT NULL,specialnost NVARCHAR(100) NOT NULL,telefon NVARCHAR(30),kabinet NVARCHAR(20),stazh INT NOT NULL DEFAULT 0 CHECK(stazh>=0),foto VARBINARY(MAX));
ELSE IF COL_LENGTH('19.10_vrachi','foto') IS NULL ALTER TABLE [19.10_vrachi] ADD foto VARBINARY(MAX);

IF OBJECT_ID(N'[19.10_lekarstva]',N'U') IS NULL
CREATE TABLE [19.10_lekarstva](id_lekarstva INT IDENTITY PRIMARY KEY,nazvanie NVARCHAR(150) NOT NULL,sposob_priema NVARCHAR(100),dozirovka NVARCHAR(100),deystvie NVARCHAR(500),pobochnye_effekty NVARCHAR(500));

IF OBJECT_ID(N'[19.10_osmotry]',N'U') IS NULL
CREATE TABLE [19.10_osmotry](id_osmotra INT IDENTITY PRIMARY KEY,id_pacienta INT NOT NULL,id_vracha INT NOT NULL,data_osmotra DATE NOT NULL,mesto_osmotra NVARCHAR(40) NOT NULL CHECK(mesto_osmotra IN(N'Амбулаторно',N'Вызов на дом')),simptomy NVARCHAR(1000),diagnoz NVARCHAR(300),predpisaniya NVARCHAR(1000),CONSTRAINT FK_1910_OsmotrPacient FOREIGN KEY(id_pacienta) REFERENCES [19.10_pacienty](id_pacienta),CONSTRAINT FK_1910_OsmotrVrach FOREIGN KEY(id_vracha) REFERENCES [19.10_vrachi](id_vracha));

IF OBJECT_ID(N'[19.10_naznacheniya]',N'U') IS NULL
CREATE TABLE [19.10_naznacheniya](id_naznacheniya INT IDENTITY PRIMARY KEY,id_osmotra INT NOT NULL,id_lekarstva INT NOT NULL,dozirovka NVARCHAR(100),sposob_priema NVARCHAR(100),kolichestvo NVARCHAR(100),CONSTRAINT FK_1910_NaznOsmotr FOREIGN KEY(id_osmotra) REFERENCES [19.10_osmotry](id_osmotra),CONSTRAINT FK_1910_NaznLek FOREIGN KEY(id_lekarstva) REFERENCES [19.10_lekarstva](id_lekarstva));

IF OBJECT_ID(N'[19.10_polzovateli]',N'U') IS NULL
CREATE TABLE [19.10_polzovateli](id_polzovatelya INT IDENTITY PRIMARY KEY,login NVARCHAR(50) UNIQUE NOT NULL,password NVARCHAR(100) NOT NULL,role NVARCHAR(40) NOT NULL CHECK(role IN(N'Администратор',N'Руководитель поликлиники',N'Оператор-врач')),is_active BIT NOT NULL DEFAULT 1);

IF OBJECT_ID(N'[19.10_audit]',N'U') IS NULL
CREATE TABLE [19.10_audit](id_zapisi INT IDENTITY PRIMARY KEY,login NVARCHAR(50),deystvie NVARCHAR(200),data_deystviya DATETIME NOT NULL DEFAULT GETDATE());

IF NOT EXISTS(SELECT 1 FROM [19.10_polzovateli] WHERE login='admin') INSERT [19.10_polzovateli](login,password,role) VALUES('admin','admin',N'Администратор');
IF NOT EXISTS(SELECT 1 FROM [19.10_polzovateli] WHERE login='director') INSERT [19.10_polzovateli](login,password,role) VALUES('director','director',N'Руководитель поликлиники');
IF NOT EXISTS(SELECT 1 FROM [19.10_polzovateli] WHERE login='doctor') INSERT [19.10_polzovateli](login,password,role) VALUES('doctor','doctor',N'Оператор-врач');

IF NOT EXISTS(SELECT 1 FROM [19.10_vrachi])
INSERT [19.10_vrachi](fio,specialnost,telefon,kabinet,stazh) VALUES
(N'Иванова Мария Сергеевна',N'Терапевт',N'+7 900 111-22-33',N'201',12),(N'Петров Алексей Игоревич',N'Хирург',N'+7 900 222-33-44',N'305',18),(N'Соколова Елена Андреевна',N'Кардиолог',N'+7 900 333-44-55',N'412',9);

IF NOT EXISTS(SELECT 1 FROM [19.10_pacienty])
INSERT [19.10_pacienty](fio,pol,data_rozhdeniya,seriya_pasporta,nomer_pasporta,polis_oms,adres,telefon) VALUES
(N'Смирнова Анна Павловна',N'Женский','1998-04-12',N'7601',N'123456',N'1234567890123456',N'г. Ярославль, ул. Свободы, 10',N'+7 901 111-22-33'),
(N'Кузнецов Дмитрий Олегович',N'Мужской','1985-11-03',N'7602',N'654321',N'9876543210987654',N'г. Ярославль, ул. Чехова, 25',N'+7 901 222-33-44'),
(N'Волкова Ирина Михайловна',N'Женский','1976-07-21',N'7603',N'789012',N'4567890123456789',N'г. Ярославль, ул. Победы, 8',N'+7 901 333-44-55');

IF NOT EXISTS(SELECT 1 FROM [19.10_lekarstva])
INSERT [19.10_lekarstva](nazvanie,sposob_priema,dozirovka,deystvie,pobochnye_effekty) VALUES
(N'Парацетамол',N'Внутрь',N'500 мг 2 раза в день',N'Жаропонижающее и обезболивающее действие',N'Аллергические реакции'),
(N'Амоксициллин',N'Внутрь',N'500 мг 3 раза в день',N'Антибактериальное действие',N'Диспепсия, аллергия'),
(N'Эналаприл',N'Внутрь',N'10 мг 1 раз в день',N'Снижение артериального давления',N'Головокружение');

IF NOT EXISTS(SELECT 1 FROM [19.10_osmotry])
INSERT [19.10_osmotry](id_pacienta,id_vracha,data_osmotra,mesto_osmotra,simptomy,diagnoz,predpisaniya) VALUES
(1,1,CAST(GETDATE() AS DATE),N'Амбулаторно',N'Повышенная температура, кашель',N'ОРВИ',N'Питьевой режим, покой'),
(2,2,DATEADD(DAY,-1,CAST(GETDATE() AS DATE)),N'Амбулаторно',N'Боль в правом боку',N'Требуется наблюдение',N'Повторный осмотр'),
(3,3,DATEADD(DAY,-2,CAST(GETDATE() AS DATE)),N'Вызов на дом',N'Повышенное давление',N'Гипертоническая болезнь',N'Контроль давления');

IF NOT EXISTS(SELECT 1 FROM [19.10_naznacheniya])
INSERT [19.10_naznacheniya](id_osmotra,id_lekarstva,dozirovka,sposob_priema,kolichestvo) VALUES(1,1,N'500 мг',N'2 раза в день после еды',N'5 дней'),(3,3,N'10 мг',N'1 раз утром',N'30 дней');

SELECT N'База данных проекта успешно подготовлена.' AS [Результат];
