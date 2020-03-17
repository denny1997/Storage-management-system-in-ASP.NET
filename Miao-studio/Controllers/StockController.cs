using Miao_studio.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Windows.Forms;

namespace Miao_studio.Controllers
{
    public class StockController : Controller
    {
        private stockInfo db = new stockInfo();
        private inputInfo db1 = new inputInfo();
        private outputInfo db2 = new outputInfo();
        // GET: Stock
        public ActionResult Index()
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login","User");
            }
            return View(db.stocks.ToList());
        }

        [HttpPost]
        public ActionResult StockDetail(string id)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            stock s = db.stocks.Find(int.Parse(id));
            var records = db1.inputs.AsQueryable();
            records = records.Where(c => ((c.name == s.name) && (c.type == s.type) && (c.unit == s.unit))).OrderByDescending(c => c.Id);
            List<input> stk = new List<input>();
            int ts = s.stock1;
            foreach (var item in records)
            {
                if (item.number >= ts)
                {
                    item.number = ts;
                    stk.Add(item);
                    break;
                }
                else
                {
                    stk.Add(item);
                    ts -= item.number;
                }
            }
            stk.Reverse();
            string details = "名称："+s.name+" | 型号："+s.type+"<br/>";
            foreach(var item in stk)
            {
                details += "入库日期：" + item.time.ToLongDateString() + " | 库存数：" + item.number.ToString() + " | 单价：" + item.price.ToString() + "<br/>";
            }
            return Content(details);
        }

        public ActionResult Input()
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }

            return View();
        }

        public ActionResult AddInput(String name, String type, String unit)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }

            return View();
        }

        [HttpPost]
        public ActionResult Input([Bind(Include = "Id,name,type,number,unit,price,time,_operator,provider,signer")] input input)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            DateTime now = DateTime.Now;
            var records = db1.inputs.AsQueryable();
            //records = records.Where(c => (c.Id.Split('-')[0]+"-"+c.Id.Split('-')[1]==now.ToString("yyyy-MM")));
            int num = 1;
            foreach(var item in records)
            {
                if (item.Id.Split('-')[0] + "-" + item.Id.Split('-')[1] == now.ToString("yyyy-MM"))
                {
                    int n = int.Parse(item.Id.Split('-')[2]);
                    if (n + 1 > num)
                        num = n + 1;
                }
            }
            //int num = records.Count()+1;
            input.Id = now.ToString("yyyy-MM") + "-" + num.ToString("000");
            input._operator = Session["user"].ToString();
            db1.inputs.Add(input);
            db1.SaveChanges();
            bool flag = true;
            foreach(var i in db.stocks.ToList())
            {
                if((i.name == input.name)&&(i.type == input.type)&&(i.unit == input.unit))
                {
                    i.stock1 += input.number;
                    i.total += input.number * input.price;
                    db.Entry(i).State = EntityState.Modified;
                    db.SaveChanges();
                    flag = false;
                    break;
                }
            }
            if(flag)
            {
                stock newRecord = new stock();
                newRecord.name = input.name;
                newRecord.type = input.type;
                newRecord.stock1 = input.number;
                newRecord.total = input.number * input.price;
                newRecord.unit = input.unit;
                db.stocks.Add(newRecord);
                db.SaveChanges();
            }
            //MessageBox.Show("入库成功", "入库提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            return RedirectToAction("Index");
        }

        public ActionResult InputDelete(string id)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }            
            input record = db1.inputs.Find(id);
            var records = db1.inputs.AsQueryable();
            records = records.Where(c => ((c.name == record.name) && (c.type == record.type) && (c.unit == record.unit))).OrderByDescending(c => c.Id);
            int sum = 0;
            foreach (var item in records)
            {
                sum += item.number;
                if (item.Id == record.Id)
                    break;
            }
            foreach (var i in db.stocks.ToList())
            {
                if ((i.name == record.name) && (i.type == record.type) && (i.unit == record.unit))
                {
                    if (i.stock1 < sum)
                    {
                        return Content("已经存在部分出库，无法删除！！");
                    }
                    i.stock1 -= record.number;
                    i.total -= record.number * record.price;
                    if(i.stock1<=0)
                    {
                        db.Entry(i).State = EntityState.Deleted;
                    }
                    else
                    {
                        db.Entry(i).State = EntityState.Modified;
                    }                 
                    db.SaveChanges();                    
                    break;
                }
            }
            db1.Entry(record).State = EntityState.Deleted;
            db1.SaveChanges();
            //MessageBox.Show("删除成功", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            return Content("删除成功");
        }

        public ActionResult InputModify(string id,string para,int type)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            input record = db1.inputs.Find(id);
            var records = db1.inputs.AsQueryable();
            records = records.Where(c => ((c.name == record.name) && (c.type == record.type) && (c.unit == record.unit))).OrderByDescending(c => c.Id);
            int sum = 0;
            foreach (var item in records)
            {
                sum += item.number;
                if (item.Id == record.Id)
                    break;
            }

            var stk = db.stocks.Where(c => ((c.name == record.name) && (c.type == record.type) && (c.unit == record.unit)));
            if (stk.Count() != 1)
                return Content("库存出现问题，请联系217-693-2649");

            switch (type)
            {
                case 1:
                    //record = db1.inputs.Find(id);
                    foreach (var i in stk)
                    {
                        if (i.stock1 < sum)
                            return Content("已经存在部分出库，无法修改！！");
                        i.stock1 -= record.number;
                        i.total -= record.number * record.price;
                        if (i.stock1 <= 0)
                        {
                            db.Entry(i).State = EntityState.Deleted;
                        }
                        else
                        {
                            db.Entry(i).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        stock newRecord = new stock();
                        newRecord.name = para;
                        newRecord.type = record.type;
                        newRecord.stock1 = record.number;
                        newRecord.total = record.number * record.price;
                        newRecord.unit = record.unit;
                        db.stocks.Add(newRecord);
                        db.SaveChanges();
                        break;                       
                    }
                    record.name = para;
                    db1.Entry(record).State = EntityState.Modified;
                    db1.SaveChanges();
                    break;

                case 2:
                    //record = db1.inputs.Find(id);
                    foreach (var i in stk)
                    {
                        if (i.stock1 < sum)
                            return Content("已经存在部分出库，无法修改！！");
                        i.stock1 -= record.number;
                        i.total -= record.number * record.price;
                        if (i.stock1 <= 0)
                        {
                            db.Entry(i).State = EntityState.Deleted;
                        }
                        else
                        {
                            db.Entry(i).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        stock newRecord = new stock();
                        newRecord.name = record.name;
                        newRecord.type = para;
                        newRecord.stock1 = record.number;
                        newRecord.total = record.number * record.price;
                        newRecord.unit = record.unit;
                        db.stocks.Add(newRecord);
                        db.SaveChanges();
                        break;
                    }
                    record.type = para;
                    //db1.Entry(record).State = EntityState.Modified;
                    //db1.SaveChanges();
                    break;

                case 3:
                    //record = db1.inputs.Find(id);     
                    foreach (var i in stk)
                    {
                        if (i.stock1 < sum)
                            return Content("已经存在部分出库，无法修改！！");
                    }
                    record.provider = para;
                    //db1.Entry(record).State = EntityState.Modified;
                    //db1.SaveChanges();
                    break;

                case 4:
                    //record = db1.inputs.Find(id);
                    foreach (var i in stk)
                    {
                        if (i.stock1 < sum)
                            return Content("已经存在部分出库，无法修改！！");
                        i.stock1 -= record.number-int.Parse(para);
                        i.total -= (record.number - int.Parse(para)) * record.price;
                        db.Entry(i).State = EntityState.Modified;
                        db.SaveChanges();                          
                        break;                       
                    }
                    record.number = int.Parse(para);
                    //db1.Entry(record).State = EntityState.Modified;
                    //db1.SaveChanges();
                    break;

                case 5:
                    //record = db1.inputs.Find(id);
                    foreach (var i in stk)
                    {
                        if (i.stock1 < sum)
                            return Content("已经存在部分出库，无法修改！！");
                        i.stock1 -= record.number;
                        i.total -= record.number * record.price;
                        if (i.stock1 <= 0)
                        {
                            db.Entry(i).State = EntityState.Deleted;
                        }
                        else
                        {
                            db.Entry(i).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        stock newRecord = new stock();
                        newRecord.name = record.name;
                        newRecord.type = record.type;
                        newRecord.stock1 = record.number;
                        newRecord.total = record.number * record.price;
                        newRecord.unit = para;
                        db.stocks.Add(newRecord);
                        db.SaveChanges();
                        break;                       
                    }
                    record.unit = para;
                    //db1.Entry(record).State = EntityState.Modified;
                    //db1.SaveChanges();
                    break;

                case 6:
                    //record = db1.inputs.Find(id);
                    foreach (var i in stk)
                    {
                        if (i.stock1 < sum)
                            return Content("已经存在部分出库，无法修改！！");
                        i.total -= record.number * (record.price-decimal.Parse(para));
                        db.Entry(i).State = EntityState.Modified;
                        db.SaveChanges();
                        break;
                    }
                    record.price = decimal.Parse(para);
                    //db1.Entry(record).State = EntityState.Modified;
                    //db1.SaveChanges();
                    break;

                case 7:
                    //record = db1.inputs.Find(id);
                    foreach (var i in stk)
                    {
                        if (i.stock1 < sum)
                            return Content("已经存在部分出库，无法修改！！");
                    }
                    record.signer = para;
                    //db1.Entry(record).State = EntityState.Modified;
                    //db1.SaveChanges();
                    break;
                case 8:
                    //record = db1.inputs.Find(id);
                    foreach (var i in stk)
                    {
                        if (i.stock1 < sum)
                            return Content("已经存在部分出库，无法修改！！");
                    }
                    record.time = Convert.ToDateTime(para);
                    //db1.Entry(record).State = EntityState.Modified;
                    //db1.SaveChanges();
                    break;
            }
            db1.Entry(record).State = EntityState.Modified;
            db1.SaveChanges();
            return Content("修改成功");
        }

        public ActionResult AddOutput(String id, String name, String type, String unit)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }

            return View();
        }

        [HttpPost]
        public ActionResult Output(string Id,string number,string project,string time)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            stock s = db.stocks.Find(int.Parse(Id));
            int num = int.Parse(number);
            if(num>s.stock1)
            {
                return Content("库存不足");
            }
            var records = db1.inputs.AsQueryable();
            records = records.Where(c => ((c.name == s.name) && (c.type == s.type) && (c.unit == s.unit))).OrderByDescending(c => c.Id);
            List<input> stk = new List<input>();
            int ts = s.stock1;
            foreach( var item in records)
            {
                if (item.number >= ts)
                {
                    item.number = ts;
                    stk.Add(item);
                    break;
                }
                else
                {
                    stk.Add(item);
                    ts -= item.number;
                }                   
            }
            stk.Reverse();
            out_put op = new out_put();
            string details = "";
            ts = num;
            decimal total = 0;
            foreach(var item in stk)
            {
                if(item.number >= ts)
                {
                    details += "数量：" + ts.ToString() + " | 单价：￥" + item.price.ToString();
                    total += ts * item.price;
                    break;
                }
                else
                {
                    details += "数量：" + item.number.ToString() + " | 单价：￥" + item.price.ToString() + "\n";
                    total += item.number * item.price;
                    ts -= item.number;
                }
            }
            DateTime now = DateTime.Now;
            var _records = db2.out_put.AsQueryable();
            //_records = _records.Where(c => ((c.time.Month == now.Month) && (c.time.Year == now.Year)));
            int _num = 1;
            foreach (var item in _records)
            {
                if (item.Id.Split('-')[0] + "-" + item.Id.Split('-')[1] == now.ToString("yyyy-MM"))
                {
                    int n = int.Parse(item.Id.Split('-')[2]);
                    if (n + 1 > _num)
                        _num = n + 1;
                }
            }
            //int _num = _records.Count() + 1;
            op.Id = now.ToString("yyyy-MM") + "-" + _num.ToString("000");
            op.name = s.name;
            op.type = s.type;
            op.detail = details;
            op.time = Convert.ToDateTime(time);
            op._operator = Session["user"].ToString();
            op.project = project;
            op.total = total;
            op.number = num;
            op.unit = s.unit;
            db2.out_put.Add(op);
            db2.SaveChanges();
            s.stock1 -= num;
            s.total -= total;
            db.Entry(s).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult OutputDelete(string id)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            out_put record = db2.out_put.Find(id);
            var _out_put = db2.out_put.Where(c => ((string.Compare(c.Id, record.Id) > 0) && (c.name == record.name) && (c.type == record.type) && (c.unit == record.unit)));
            if (_out_put.Count() > 0)
                return Content("已存在后续出库操作，无法删除！！");
            foreach (var i in db.stocks.ToList())
            {
                if ((i.name == record.name) && (i.type == record.type) && (i.unit == record.unit))
                {
                    i.stock1 += record.number;
                    i.total += record.total;                 
                    db.Entry(i).State = EntityState.Modified;                   
                    db.SaveChanges();
                    break;
                }
            }
            db2.Entry(record).State = EntityState.Deleted;
            db2.SaveChanges();
            //MessageBox.Show("删除成功", "操作提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            return Content("删除成功");
        }

        public ActionResult OutputModify(string id, string para, int type)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            out_put record = db2.out_put.Find(id);
            switch (type)
            {
                case 1:
                    record.time = Convert.ToDateTime(para);
                    break;
                case 2:
                    record.project = para;
                    break;
            }
            db2.Entry(record).State = EntityState.Modified;
            db2.SaveChanges();
            return Content("修改成功");
        }

            public ActionResult OutputResult(string id, string name, string type, DateTime? begin, DateTime? end, string project)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            SearchInfo info = new SearchInfo(id, name, type, begin, end, project);
            var op = db2.out_put.AsQueryable();
            if(!String.IsNullOrEmpty(id))
            {
                op = op.Where(c => c.Id.Contains(id));
            }
            if(!String.IsNullOrEmpty(name))
            {
                op = op.Where(c => c.name.Contains(name));
            }
            if(!String.IsNullOrEmpty(type))
            {
                op = op.Where(c => c.type.Contains(type));
            }
            if (!String.IsNullOrEmpty(project))
            {
                op = op.Where(c => c.project.Contains(project));
            }
            if (begin!=null)
            {
                op = op.Where(c => c.time >= begin);
            }
            if(end!=null)
            {
                op = op.Where(c => c.time <= end);
            }
            return View(Tuple.Create(op.AsEnumerable(),info));
        }

        public ActionResult InputResult(string id, string name, string type, DateTime? begin, DateTime? end,string provider)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            SearchInfo info = new SearchInfo(id, name, type, begin, end, provider);
            var ip = db1.inputs.AsQueryable();
            if (!String.IsNullOrEmpty(id))
            {
                ip = ip.Where(c => c.Id.Contains(id));
            }
            if (!String.IsNullOrEmpty(name))
            {
                ip = ip.Where(c => c.name.Contains(name));
            }
            if (!String.IsNullOrEmpty(type))
            {
                ip = ip.Where(c => c.type.Contains(type));
            }
            if (!String.IsNullOrEmpty(provider))
            {
                ip = ip.Where(c => c.provider.Contains(provider));
            }
            if (begin != null)
            {
                ip = ip.Where(c => c.time >= begin);
            }
            if (end != null)
            {
                ip = ip.Where(c => c.time <= end);
            }
            return View(Tuple.Create(ip.AsEnumerable(), info));
        }

        public ActionResult StockResult(string name, string type, DateTime? begin, DateTime? end)
        {
            if (Session["user"] == null)
            {
                //MessageBox.Show("请先登录", "登录提示", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                return RedirectToAction("Login", "User");
            }
            
            SearchInfo info = new SearchInfo(name, type, begin, end);
            var stk = db.stocks.AsQueryable().AsEnumerable();           
            var ip = db1.inputs.AsQueryable().AsEnumerable();
            var op = db2.out_put.AsQueryable().AsEnumerable();
            if (!String.IsNullOrEmpty(name))
            {
                stk = stk.Where(c => c.name.Contains(name));
                ip = ip.Where(c => c.name.Contains(name));
                op = op.Where(c => c.name.Contains(name));
            }
            if (!String.IsNullOrEmpty(type))
            {
                stk = stk.Where(c => c.type.Contains(type));
                ip = ip.Where(c => c.name.Contains(type));
                op = op.Where(c => c.name.Contains(type));
            }

            if (begin != null)
            {
                ip = ip.Where(c => c.time >= begin);
                op = op.Where(c => c.time >= begin);
            }
            //List<stock> t1 = stk.ToList();
            List<StockResult> former = RetroSpect(stk, ip, op);
           
            List<StockResult> latter;
            if (end != null)
            {
                end = end.Value.AddMonths(1);
                ip = ip.Where(c => c.time >= end);
                op = op.Where(c => c.time >= end);
                latter = RetroSpect(stk, ip, op);
            }
            else
            {
                latter = RetroSpect(stk, null, null);
            }
            
           
            return View(Tuple.Create(former, latter, info));
        }

        public FileResult ExportOutputResult()
        {
            NPOI.HSSF.UserModel.HSSFWorkbook outputExcel = new NPOI.HSSF.UserModel.HSSFWorkbook();
            NPOI.SS.UserModel.ISheet sheet1 = outputExcel.CreateSheet("Sheet1");
            NPOI.SS.UserModel.IRow row1 = sheet1.CreateRow(0);
            row1.CreateCell(0).SetCellValue("出库单编号");
            row1.CreateCell(1).SetCellValue("名称");
            row1.CreateCell(2).SetCellValue("型号");
            row1.CreateCell(3).SetCellValue("日期");
            row1.CreateCell(4).SetCellValue("操作人");
            row1.CreateCell(5).SetCellValue("项目");
            row1.CreateCell(6).SetCellValue("数量");
            row1.CreateCell(7).SetCellValue("单位");
            row1.CreateCell(8).SetCellValue("总金额");
            row1.CreateCell(9).SetCellValue("金额明细");

            List<out_put> res = (List<out_put>)Session["excelOutput"];
            for (int i = 0; i < res.Count; i++)
            {
                NPOI.SS.UserModel.IRow rowtemp = sheet1.CreateRow(i + 1);
                rowtemp.CreateCell(0).SetCellValue(res[i].Id.ToString());
                rowtemp.CreateCell(1).SetCellValue(res[i].name.ToString());
                rowtemp.CreateCell(2).SetCellValue(res[i].type.ToString());
                rowtemp.CreateCell(3).SetCellValue(res[i].time.ToString());
                rowtemp.CreateCell(4).SetCellValue(res[i]._operator.ToString());
                rowtemp.CreateCell(5).SetCellValue(res[i].project.ToString());
                rowtemp.CreateCell(6).SetCellValue(res[i].number.ToString());
                rowtemp.CreateCell(7).SetCellValue(res[i].unit.ToString());
                rowtemp.CreateCell(8).SetCellValue(res[i].total.ToString());
                rowtemp.CreateCell(9).SetCellValue(res[i].detail.ToString());
            }

            MemoryStream ms = new MemoryStream();
            outputExcel.Write(ms);
            ms.Seek(0, SeekOrigin.Begin);

            string fileName = "出库记录表.xls";
            return File(ms, "application/vnd.ms-excel", fileName);
        }

        public FileResult ExportInputResult()
        {
            NPOI.HSSF.UserModel.HSSFWorkbook inputExcel = new NPOI.HSSF.UserModel.HSSFWorkbook();
            NPOI.SS.UserModel.ISheet sheet1 = inputExcel.CreateSheet("Sheet1");
            NPOI.SS.UserModel.IRow row1 = sheet1.CreateRow(0);            
            row1.CreateCell(0).SetCellValue("入库单编号");
            row1.CreateCell(1).SetCellValue("名称");
            row1.CreateCell(2).SetCellValue("型号");
            row1.CreateCell(3).SetCellValue("供应商");
            row1.CreateCell(4).SetCellValue("日期");
            row1.CreateCell(5).SetCellValue("操作人");
            row1.CreateCell(6).SetCellValue("数量");
            row1.CreateCell(7).SetCellValue("单位");
            row1.CreateCell(8).SetCellValue("单价");
            row1.CreateCell(9).SetCellValue("总金额");
            row1.CreateCell(10).SetCellValue("签收人");

            List<input> res = (List<input>)Session["excelInput"];
            for (int i = 0; i < res.Count; i++)
            {
                NPOI.SS.UserModel.IRow rowtemp = sheet1.CreateRow(i + 1);
                rowtemp.CreateCell(0).SetCellValue(res[i].Id.ToString());
                rowtemp.CreateCell(1).SetCellValue(res[i].name.ToString());
                rowtemp.CreateCell(2).SetCellValue(res[i].type.ToString());
                rowtemp.CreateCell(3).SetCellValue(res[i].provider.ToString());
                rowtemp.CreateCell(4).SetCellValue(res[i].time.ToString());
                rowtemp.CreateCell(5).SetCellValue(res[i]._operator.ToString());
                rowtemp.CreateCell(6).SetCellValue(res[i].number.ToString());
                rowtemp.CreateCell(7).SetCellValue(res[i].unit.ToString());
                rowtemp.CreateCell(8).SetCellValue(res[i].price.ToString());
                rowtemp.CreateCell(9).SetCellValue((res[i].number * decimal.ToDouble(res[i].price)).ToString());
                if (res[i].signer != null)
                    rowtemp.CreateCell(10).SetCellValue(res[i].signer.ToString());
                else
                    rowtemp.CreateCell(10).SetCellValue("");
            }

            MemoryStream ms = new MemoryStream();
            inputExcel.Write(ms);
            ms.Seek(0, SeekOrigin.Begin);

            string fileName = "入库记录表.xls";
            return File(ms, "application/vnd.ms-excel", fileName);
        }
        public FileResult ExportStockExcel(DateTime begin, DateTime end)
        {
            NPOI.HSSF.UserModel.HSSFWorkbook stockExcel = new NPOI.HSSF.UserModel.HSSFWorkbook();
            NPOI.SS.UserModel.ISheet sheet1 = stockExcel.CreateSheet("Sheet1");
            NPOI.SS.UserModel.IRow row1 = sheet1.CreateRow(0);
            row1.CreateCell(0).SetCellValue("名称");
            row1.CreateCell(1).SetCellValue("型号");
            row1.CreateCell(2).SetCellValue("单位");
            row1.CreateCell(3).SetCellValue("期初库存");
            row1.CreateCell(4).SetCellValue("期初总金额");
            row1.CreateCell(5).SetCellValue("期间入库数量");
            row1.CreateCell(6).SetCellValue("期间入库金额");
            row1.CreateCell(7).SetCellValue("期间出库数量");
            row1.CreateCell(8).SetCellValue("期间出库金额");
            row1.CreateCell(9).SetCellValue("期末库存");
            row1.CreateCell(10).SetCellValue("期末总金额");

            List<StockExcel> res = (List<StockExcel>)Session["excel"];
            for (int i = 0; i < res.Count; i++)
            {
                NPOI.SS.UserModel.IRow rowtemp = sheet1.CreateRow(i + 1);
                rowtemp.CreateCell(0).SetCellValue(res[i].name.ToString());
                rowtemp.CreateCell(1).SetCellValue(res[i].type.ToString());
                rowtemp.CreateCell(2).SetCellValue(res[i].unit.ToString());
                rowtemp.CreateCell(3).SetCellValue(res[i].former_stock.ToString());
                rowtemp.CreateCell(4).SetCellValue(res[i].former_total.ToString());
                rowtemp.CreateCell(5).SetCellValue(res[i].delta_inputstock.ToString());
                rowtemp.CreateCell(6).SetCellValue(res[i].delta_inputtotal.ToString());
                rowtemp.CreateCell(7).SetCellValue(res[i].delta_outputstock.ToString());
                rowtemp.CreateCell(8).SetCellValue(res[i].delta_outputtotal.ToString());
                rowtemp.CreateCell(9).SetCellValue(res[i].latter_stock.ToString());
                rowtemp.CreateCell(10).SetCellValue(res[i].latter_total.ToString());
            }

            MemoryStream ms = new MemoryStream();
            stockExcel.Write(ms);
            ms.Seek(0, SeekOrigin.Begin);

            string fileName = begin.ToString("yyyy-MM") + "到" + end.ToString("yyyy-MM") + "库存汇总表.xls";
            return File(ms, "application/vnd.ms-excel", fileName);
        }

        private List<StockResult> RetroSpect(IEnumerable<stock> stock, IEnumerable<input> input, IEnumerable<out_put> output)
        {
            List<StockResult> res = new List<StockResult>();
            foreach (var item in stock)
            {
                int temp_stk = item.stock1;
                decimal temp_total = item.total;
                int temp_dstock = 0;
                decimal temp_dtotal = 0;
                if ((input!=null)&&(output!=null))
                {
                    var temp_ip = input.Where(c => ((c.name == item.name) && (c.type == item.type) && (c.unit == item.unit)));
                    var temp_op = output.Where(c => ((c.name == item.name) && (c.type == item.type) && (c.unit == item.unit)));                    
                    foreach (var it in temp_ip)
                    {
                        temp_stk -= it.number;
                        temp_total -= it.number * it.price;
                        temp_dstock += it.number;
                    }
                    foreach (var it in temp_op)
                    {
                        temp_stk += it.number;
                        temp_total += it.total;
                        temp_dtotal += it.total;
                    }
                }               
                res.Add(new StockResult(item.name,item.type,item.unit,temp_stk,temp_total,temp_dstock,temp_dtotal));
            }
            return res;
        }
    }
}