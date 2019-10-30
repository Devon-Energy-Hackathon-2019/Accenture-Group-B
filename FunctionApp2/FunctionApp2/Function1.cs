using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace FunctionApp2
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("*/5 * * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            string connetionString = null;
            //SqlConnection cnn;
            connetionString = "Data Source=accenturegroupbsqlserver.database.windows.net;Initial Catalog=pipechallenge;User ID=accenturegroupb;Password=Dvnhack2019";
            //cnn = new SqlConnection(connetionString);
            StringBuilder sql = new StringBuilder();
            sql.Append("select * from [dbo].[PipeCount] WITH(NOLOCK) where status = 0");
            DataSet pipereq = new DataSet();

            try
            {
                using (SqlConnection connection = new SqlConnection(connetionString))
                {
                    connection.Open();
                    Console.WriteLine("Connection Open ! ");
                    SqlDataAdapter adapter = new SqlDataAdapter(sql.ToString(), connection);

                    adapter.Fill(pipereq, "PipeList");
                }

                foreach (DataRow row in pipereq.Tables[0].Rows)
                {
                    byte[] byteData = (byte[])row["rackimage"];
                    int rackid = (int)row["id"];
                    MakePredictionRequest(byteData, rackid);
                }

                Console.WriteLine("\n\nHit ENTER to exit...");
                Console.ReadLine();
            }

            catch (Exception ex)
            {
                
            }


        }
        public static async Task MakePredictionRequest(byte[] byteData, int rackid)
        {
            var client = new HttpClient();

            // Request headers - replace this example key with your valid Prediction-Key.
            //client.DefaultRequestHeaders.Add("Prediction-Key", "02da455440914c5ea853084bd4ce50fc");

            // Prediction URL - replace this example URL with your valid Prediction URL.
            //string url = "https://southcentralus.api.cognitive.microsoft.com/customvision/v3.0/Prediction/3b4b43ac-ef13-492c-ab8f-424c1f637c96/detect/iterations/Iteration1/image";

            client.DefaultRequestHeaders.Add("Prediction-Key", "02da455440914c5ea853084bd4ce50fc");
            string url = "https://southcentralus.api.cognitive.microsoft.com/customvision/v3.0/Prediction/3b4b43ac-ef13-492c-ab8f-424c1f637c96/detect/iterations/Iteration3/image";

            HttpResponseMessage response;
            var CollectionWordsOri = string.Empty;

            // Request body. Try this sample with a locally stored image.
            //byte[] byteData = GetImageAsByteArray(imageFilePath);
            var count = 0;
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);
                //Console.WriteLine(await response.Content.ReadAsStringAsync());
                ////var CollectionWords = response.Content.ReadAsStringAsync().Result.Split('{');
                //string[] words = CollectionWords.Split('{');
                CollectionWordsOri = response.Content.ReadAsStringAsync().Result;
                var CollectionWords = CollectionWordsOri.ToString().Split('{');

                var per = 0.00;
                foreach (var word in CollectionWords)
                {
                    //System.Console.WriteLine($"<{word}>");
                    if (word.Contains("probability"))
                    {
                        per = Convert.ToDouble(word.Substring(14, 5));
                        if (per > 0.5)
                        {
                            Console.WriteLine(word.Substring(14, 5));
                            count += 1;
                        }

                    }

                }
                Console.WriteLine(count);
            }

           

            var connetionString = "Data Source=accenturegroupbsqlserver.database.windows.net;Initial Catalog=pipechallenge;User ID=accenturegroupb;Password=Dvnhack2019";
            //var sql = "UPDATE PipeCount SET status = @status, pipecount = @pipecount where id = @id";// repeat for all variables
            var sql = "UPDATE PipeCount SET status = @status, pipecount = @pipecount,JsonString = @JsonString where id = @id";// repeat for all variables

            try
            {
                using (var connection = new SqlConnection(connetionString))
                {
                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.Add("@status", SqlDbType.Bit).Value = 1;
                        command.Parameters.Add("@pipecount", SqlDbType.Int).Value = count;
                        command.Parameters.Add("@id", SqlDbType.Int).Value = rackid;
                        command.Parameters.Add("@JsonString", SqlDbType.VarChar).Value = CollectionWordsOri;
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                
            }
         


        }
        private static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

    }
}

