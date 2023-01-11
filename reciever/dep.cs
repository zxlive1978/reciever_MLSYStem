using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace reciever
{
    public class dep
    {
        //Триггер пуска
        public bool start =false;
        //Путь к источнику
        public string src;
        //Путь к получателю
        public string dst;
        //Текущий time имя
        public string time;
        //Текущее расширение
        public string ext_time;
        //Номер текущей записи
        public int numb_cur_lst_record;
        //Содержимое текущей записи в lst
        public List<byte[]> cur_lst_record=new List<byte[]>();
        //Содержимое текущей записи в dep
        public List<byte[]> cur_dep_record = new List<byte[]>();

        //Число записей в источнике
        public int src_numbs_record;
        //Число записей в получателе
        public int dst_numbs_record;
        //Cообщение об ошибке
        public string error;
        //Кол-во записей для чтения из источника за один раз

        /////////////////////////////////////////////////////
        //LST запись
        //Базовое смещение от начала файла
        public uint base_disp;
        //Базовое смещение от начала файла последней записи
        public uint base_disp2_last;
        //Значение ключевого поля
        public float key_value;
        //Время (DOS формат)
        public uint time_reg;
        //Номер записи (истинный)
        public uint numb_true;
        //Номер записи (логический)
        public uint numb_log;
        //Флаг состояния
        //0-нормальная
        //1-отмеченная
        //2-удаленная
        public byte status_flag;

        /////////////////////////////////////////////////////
        //DEP запись
        //Заголовок
        //Длинна записи в байтах (включая данный заголовок)
        public ushort length_record_in_byte;
        public byte[] length_record_in_byte_mass = new byte[2];

        //Длинна записи в байтах (включая данный заголовок) последней записи
        public ushort length_record_in_byte_last;
        public byte[] length_record_in_byte_mass_last = new byte[2];

        //Кол-во записей для четния(записи) за один раз 
        public int now_src_numbs_record;

        //Переменная для тестовой проверки базвого смещения
        public uint disp_base_test;
        public uint disp2_base_last_test;
        public int now_src_numbs_record_good;


        public dep() { 
        }

        //Чтение lst записи источника
        public void src_read_lst_record()
        {
          
            string path = src + "\\" + time + ".lst";
            check_path(src);
            check_file(path);
            if (error != "") { }
            else
            {
                if (cur_lst_record.Count != 0)
                {
                    cur_lst_record.Clear();
                }
                try
                {

                    using (FileStream b = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {

                        //Если число записей за раз больше, чем число оставшихся записей, тогда читаем оставшиеся записи
                        if ( src_numbs_record-numb_cur_lst_record <now_src_numbs_record) {
                            now_src_numbs_record = src_numbs_record - numb_cur_lst_record;
                        }

                        byte[] lst_record = new byte[now_src_numbs_record * 21];
                        b.Seek(numb_cur_lst_record*21,SeekOrigin.Begin);
                        b.Read(lst_record, 0, now_src_numbs_record * 21);
                        cur_lst_record.Add(lst_record);
                        //b.Close();
                        //b.Dispose();
                        //Базовое смещение от начала файла
                        base_disp = BitConverter.ToUInt32(cur_lst_record[0], 0);

                        base_disp2_last = BitConverter.ToUInt32(cur_lst_record[0], now_src_numbs_record * 21 - 21);
                        //Номер записи (истинный)
                        numb_true = BitConverter.ToUInt32(cur_lst_record[0], 0 +
                        4 + 4 + 4);

                        int count=0;
                        //Проверка на возрастание базового адреса смещения
                        if (lst_record.Length>21){
                        for (int i = 0; i < lst_record.Length-21; i=i+21) {
                            disp_base_test = BitConverter.ToUInt32(lst_record, i);
                            disp2_base_last_test = BitConverter.ToUInt32(lst_record, i+21);

                            if (disp_base_test < disp2_base_last_test)
                            {
                                
                                base_disp2_last = disp2_base_last_test;
                                count = i;
                               
                                //now_src_numbs_record_good = i+1;
                            }
                            else {
                                break;
                            
                            }}
 
                        
                        }

                        if (count!=0){
                        byte[] lst_record3 = new byte[count];
                        b.Seek(numb_cur_lst_record * 21, SeekOrigin.Begin);
                        b.Read(lst_record3, 0, count);
                        cur_lst_record.Clear();
                        cur_lst_record.Add(lst_record);
                        }
                          
                        //Если смещение базы больше? Читаем одну запись
                        if (base_disp >= base_disp2_last) {

                            byte[] lst_record2 = new byte[now_src_numbs_record * 21];
                            b.Seek(numb_cur_lst_record * 21, SeekOrigin.Begin);
                            b.Read(lst_record2, 0, 21);
                            cur_lst_record.Clear();
                            cur_lst_record.Add(lst_record);
                            base_disp2_last = base_disp;
                            
                        }
                    }
                }
                catch (FileNotFoundException err) { error = err.ToString(); }
                catch (IOException err) { error = err.ToString(); }
            }
        }

        //Чтение dep записи источника
        public void src_read_dep_record()
        {
            string path = src + "\\" + time + ".dep";
            check_path(src);
            check_file(path);
            if (error != "") {  }
            else
            {
                if (cur_dep_record.Count != 0)
                {
                    cur_dep_record.Clear();
                }
                try
                {
                    using (FileStream b = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {
                        //Поиск длины записи в байтах (включая данный заголовок)
                        b.Seek( (base_disp + 6),SeekOrigin.Begin);
                        b.Read(length_record_in_byte_mass,0, 2);
                        length_record_in_byte = BitConverter.ToUInt16(length_record_in_byte_mass, 0);

                        //Поиск длины записи в байтах (включая данный заголовок) последняя запись
                        b.Seek((base_disp2_last + 6), SeekOrigin.Begin);
                        b.Read(length_record_in_byte_mass_last, 0, 2);
                        length_record_in_byte_last = BitConverter.ToUInt16(length_record_in_byte_mass_last, 0);


                        //Чтение в массив всей записи DEP
                        byte[] one_record_dep = new byte[base_disp2_last - base_disp + length_record_in_byte_last];
                        b.Seek(base_disp, SeekOrigin.Begin);
                        b.Read(one_record_dep, 0, (int)(base_disp2_last - base_disp + length_record_in_byte_last));
                        cur_dep_record.Add(one_record_dep);
                        //b.Close();
                        //b.Dispose();
                    }

                }
                catch (FileNotFoundException err) { error = err.ToString(); }
                catch (IOException err) { error = err.ToString(); }
            }
        }

        //Запись lst записи приемника
        public void dst_write_lst_record()
        {
            string path = dst + "\\" + time + ".lst";
            check_path(dst);
            //check_file(path);
            if (error != "") { }
            else
            {
                //if (cur_lst_record.Count != 0)
                //{
                //    cur_lst_record.Clear();
                //}
                try
                {

                    using (FileStream b = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {

                        //FileInfo s = new FileInfo(path);
                        ////byte[] lst_record = new byte[21];
                        b.Seek(0, SeekOrigin.End);
                        b.Write(cur_lst_record[0], 0, cur_lst_record[0].Length);
                        b.Flush();
                        b.Close();
                        b.Dispose();

                        //Базовое смещение от начала файла
                        //base_disp = BitConverter.ToUInt32(cur_lst_record[0], 0);
                        ////Номер записи (истинный)
                        //numb_true = BitConverter.ToUInt32(cur_lst_record[0], 0 +
                        //    4 + 4 + 4);
                    }
                }
                catch (FileNotFoundException err) { error = err.ToString(); }
                catch (IOException err) { error = err.ToString(); }
            }
        }

        //Запись dep записи приемника
        public void dst_write_dep_record()
        {
            string path = dst + "\\" + time + ".dep";
            check_path(dst);
            //check_file(path);
            if (error != "") { }
            else
            {
                //if (cur_dep_record.Count != 0)
                //{
                //    cur_dep_record.Clear();
                //}
                try
                {
                    using (FileStream b = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        ////Поиск длины записи в байтах (включая данный заголовок)
                        //b.Seek((int)(base_disp + 6), SeekOrigin.Begin);
                        //b.Read(length_record_in_byte_mass, 0, 2);
                        //length_record_in_byte = BitConverter.ToUInt16(length_record_in_byte_mass, 0);

                        ////Чтение в массив всей записи DEP
                        //byte[] one_record_dep = new byte[length_record_in_byte];
                        //b.Seek((int)base_disp, SeekOrigin.Begin);
                        //b.Read(one_record_dep, 0, length_record_in_byte);
                        //cur_dep_record.Add(one_record_dep);
                        //b.Dispose();
                        b.Seek((int)base_disp, SeekOrigin.Begin);
                        b.Write(cur_dep_record[0], 0, cur_dep_record[0].Length);
                        b.Flush();
                        b.Close();
                        b.Dispose();

                    }

                }
                catch (FileNotFoundException err) { error = err.ToString(); }
                catch (IOException err) { error = err.ToString(); }
            }
        }

        
        //Проверка пути
        public void check_path(string p1){

            if (!Directory.Exists(p1)) { 
                error = "Все пропало, нет пути: " + p1; } 
            else {
                //error = "";
            }
        }

        //Проверка файла
        public void check_file(string p1) {
            if (!File.Exists(p1))
            { 
                error = "Все пропало, нет файла: " + p1; }
            else
            {
                //error = "";
            }
        
        }


        //Перессылка
        public void transfer() { 
            if (start) {

                
            }}

        //Подсчет числа записей lst источника
        public void calc_src_numbs_record()
        {
            string path = src + "\\" + time + ".lst";
            check_path(src);
            check_file(path);
            if (error != "") { }
            else
            {
                try
                {
                    FileInfo b = new FileInfo(path);                    
                    src_numbs_record = (int)b.Length/21;
                   

                }
                catch (FileNotFoundException err) { error = err.ToString(); }
                catch (IOException err) { error = err.ToString(); }


            }
        }

        //Подсчет числа записей lst приемника
        public void calc_dst_numbs_record()
        {
            string path = dst + "\\" + time + ".lst";
            check_path(dst);
            //check_file(path);
            if (error != "") { }
            else
            {
                try
                {
                    if (File.Exists(path))
                    {
                        FileInfo b = new FileInfo(path);
                        dst_numbs_record = (int)b.Length / 21;
                    }
                    else {
                        //Новый файл 
                        dst_numbs_record = 0; }

                }
                catch (FileNotFoundException err) { error = err.ToString(); }
                catch (IOException err) { error = err.ToString(); }
            }
        }
        
        }
    
}
