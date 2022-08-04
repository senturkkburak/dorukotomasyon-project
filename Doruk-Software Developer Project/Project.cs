using Oracle.DataAccess.Client;
using System;
using System.Collections;
using System.Data;
using System.Windows.Forms;

namespace Doruk_Software_Developer_Project
{
    public partial class Project : Form
    {
        public OracleConnection conn;
        public Project()
        {
            InitializeComponent();
        }
        public string OracleConnString(string host, string port, string servicename, string user, string pass)
        {
            return String.Format(
              "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})" +
              "(PORT={1}))(CONNECT_DATA=(SERVICE_NAME={2})));User Id={3};Password={4};",
              host,
              port,
              servicename,
              user,
              pass);
        }
        public void CheckConnection(string connectionstring)
        {
            try
            {
                using (conn = new OracleConnection(connectionstring))
                {
                    MessageBox.Show("Giriş Başarılı..");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oracle bağlantısı kurulamadı... Tekrar deneyiniz.");
                throw ex;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            string connectionstring = OracleConnString("localhost", "1521", "XE", "system", "burak123"); //Kullanıcı Adı: system , Şifre: burak123 olarak ayarlı
            CheckConnection(connectionstring);
            conn = new OracleConnection(connectionstring);
            conn.Open();
            OracleCommand command = new OracleCommand();
            command.CommandText = "select distinct STOPPAGEREASON from STOPPAGE";
            command.Connection = conn;
            OracleDataReader reader = command.ExecuteReader();
            OracleDataAdapter da = new OracleDataAdapter(command);
            DataTable table = new DataTable();                             //Ana Tablo
            DataTable tableColumnNames = new DataTable();                  //Duruş Sebepleri Databaseden Distinct şekilde çekilip Ana tabloya column name olarak yazılıyor
            da.Fill(tableColumnNames);                                     //Dinamik bir yapıda olması sebebiyle veritabanına eklenen bir duruş nedeni, tabloya sütun olarak ekleniyor
            table.Columns.Add("İş Emri");
            foreach (DataRow dr in tableColumnNames.Rows)
            {
                table.Columns.Add(dr["STOPPAGEREASON"].ToString());
            }
            table.Columns.Add("Toplam");
            DataTable orderList = new DataTable();
            command.CommandText = "select * from WORKORDER";               //İş Emri Tablosu çekiliyor
            TimeSpan zamanAraligi;
            da = new OracleDataAdapter(command);
            da.Fill(orderList);                                            //İş Emri Tablosu
            OracleCommand command2 = new OracleCommand();
            command2.CommandText = "select * from stoppage ";              //Duruş Tablosu çekiliyor
            command2.Connection = conn;
            DataTable durusList = new DataTable();
            OracleDataAdapter da1 = new OracleDataAdapter(command2);
            da1.Fill(durusList);                                          //Duruş Tablosu
            int dakika = 0;



            foreach (DataRow row1 in orderList.Rows)                       //İş Emri numaralarını ilk columna yazan döngü 
            {                                                             //Dinamik bir yapı olduğundan dolayı database'e eklenen bir iş emri tabloya satır olarak ekleniyor
                table.Rows.Add(row1["orderno"].ToString());
            }
            foreach (DataRow order in orderList.Rows)                     //En dıştaki döngü: İş Emri tablosundaki rowlar kadar dönüyor
            {
                DateTime isemriBaslangic = (DateTime)order["startdate"];  // İş emri date dönüşümleri
                DateTime isemriBitis = (DateTime)order["enddate"];
                int durusDakika = 0;                                      //Dakika cinsinden duruş süresi
                foreach (DataRow durus in durusList.Rows)                 //Duruş listesindeki rowları dönen döngü
                {
                    DateTime durusBaslangic = (DateTime)durus["startdate"];
                    DateTime durusBitis = (DateTime)durus["enddate"];

                    if (isemriBaslangic <= durusBaslangic && durusBitis <= isemriBitis)  //Eğer duruşun başlangıcı ve bitişi , iş emrinin içindeyse
                    {
                        zamanAraligi = durusBitis - durusBaslangic;                      //Zaman aralığı hesabı
                        dakika = (int)zamanAraligi.TotalMinutes;                         //Dakika çevirimi
                        durusDakika += dakika;                                           // Şu anki iş emrindeki duruş sürelerinin toplandığı değişken

                    }
                    else if (durusBaslangic <= isemriBaslangic && isemriBitis <= durusBitis)   //Önceki iş emrinde başlayıp şu an kontrol edilen iş emrinin içinde biten duruş
                    {
                        zamanAraligi = isemriBitis - isemriBaslangic;             //Şu anki iş emrindeki geçirdiği sürenin hesaplanması
                        dakika = (int)zamanAraligi.TotalMinutes;
                        durusDakika += dakika;

                    }
                    else if (isemriBitis >= durusBaslangic && durusBitis >= isemriBitis) //Şu anki iş emrinden sonraki bir emire uzayan duruş
                    {
                        zamanAraligi = isemriBitis - durusBaslangic;                     //Şu anki iş emrinde geçen duruş süresi için
                        dakika = (int)zamanAraligi.TotalMinutes;
                        durusDakika += dakika;

                    }
                    else if (isemriBaslangic <= durusBitis && isemriBitis >= durusBaslangic) //Önceki bir emirden başlayıp sonraki bir emire kadar süren duruş hesaplaması
                    {
                        zamanAraligi = durusBitis - isemriBaslangic;
                        dakika = (int)zamanAraligi.TotalMinutes;
                        durusDakika += dakika;

                    }
                    else
                    {
                        continue;
                    }
                    foreach (DataRow anaTabloRow in table.Rows)                      //Ana tabloyu dönen döngü
                    {
                        if (anaTabloRow["İş Emri"].ToString() == order["orderno"].ToString()) //Column ismine bakarak duruş süresini uygun columna ekle
                        {
                            anaTabloRow["Toplam"] = durusDakika;                          //Duruş süresini rowun sonundaki toplam columnuna yazıyor
                            String durusStr = (string)durus["stoppagereason"].ToString();
                            if (anaTabloRow[durusStr] != DBNull.Value)
                            {
                                int x = Convert.ToInt32((anaTabloRow[durusStr]));        //Eğer tablodaki hücre boş değilse var olan değeri al, eklenecek değeri toplayıp yaz
                                anaTabloRow[durusStr] = x + dakika;
                            }
                            else
                            {
                                anaTabloRow[durusStr] = dakika;                         //Hücre değeri boşsa direkt yazılacak değeri yaz
                            }
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            dataTable.DataSource = table;                            //Tablo oluştuktan sonra her bir columndaki değerlerin toplanıp sırayla son satıra yazılması
            int sumColumn = 0;                                       //Ve null değerler yerine 0 yazılması
            int xTable = 0;
            int yTable = 0;
            ArrayList ar = new ArrayList();
            foreach (DataColumn col in table.Columns)
            {
                sumColumn = 0;
                foreach (DataRow row in table.Rows)
                {
                    if (yTable != 0 && xTable != 0 && row[col] != DBNull.Value)
                    {
                        sumColumn += Convert.ToInt32(row[col]);
                    }
                    else if (row[col] == DBNull.Value)
                    {
                        row[col] = 0;
                    }
                    else
                    {
                    }
                    xTable++;
                }
                yTable++;
                ar.Add(sumColumn);
            }
            ar[0] = "Toplam";
            table.Rows.Add(ar.ToArray());                         //Oluşturulan toplam sayıları içeren rowun insertionu
        }
    }
}
