using Microsoft.AspNetCore.Mvc;
using DRB_HMI_3D.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DRB_HMI_3D.Controllers
{
    public class PressController : Controller
    {
        private readonly KepwareService _kepwareService;

        public PressController(KepwareService kepwareService)
        {
            _kepwareService = kepwareService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMachines()
        {
            try
            {
                var data = new List<object>();

                for (int i = 1; i <= 6; i++)
                {
                    string ctl = $"CTL{i}";

                    var tagList = new List<string>
                    {
                        $"ns=2;s=CTL.{ctl}.START/STOP",
                        $"ns=2;s=CTL.{ctl}.THOI GIAN LUU HOA",
                        $"ns=2;s=CTL.{ctl}.NHIET DO MAM TREN",
                        $"ns=2;s=CTL.{ctl}.NHIET DO MAM GIUA",
                        $"ns=2;s=CTL.{ctl}.NHIET DO MAM DUOI",
                        $"ns=2;s=CTL.{ctl}.AP LUC"
                    };

                    var values = await _kepwareService.ReadMultipleTagsAsync(tagList);

                    // Chuyển kiểu an toàn
                    int startStop = values[tagList[0]] != null ? Convert.ToInt32(values[tagList[0]]) : 0;
                    int thoiGianLuuHoa = values[tagList[1]] != null ? Convert.ToInt32(values[tagList[1]]) : 0;
                    double nhietDoMamTren = values[tagList[2]] != null ? Convert.ToDouble(values[tagList[2]]) / 10 : 0;
                    double nhietDoMamGiua = values[tagList[3]] != null ? Convert.ToDouble(values[tagList[3]]) / 10 : 0;
                    double nhietDoMamDuoi = values[tagList[4]] != null ? Convert.ToDouble(values[tagList[4]]) / 10 : 0;
                    double apLuc = values[tagList[5]] != null ? Convert.ToDouble(values[tagList[5]]) / 10 : 0;

                    data.Add(new
                    {
                        machine = ctl,
                        startStop = startStop,
                        thoiGianLuuHoa = thoiGianLuuHoa,
                        nhietDoMamTren = nhietDoMamTren,
                        nhietDoMamGiua = nhietDoMamGiua,
                        nhietDoMamDuoi = nhietDoMamDuoi,
                        apLuc = apLuc
                    });
                }

                return Json(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Lỗi kết nối Kepware",
                    detail = ex.Message
                });
            }
        }
    }
}