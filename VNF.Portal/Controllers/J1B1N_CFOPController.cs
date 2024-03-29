﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VNF.Business;
using System.Data;
using MetsoFramework.Utils;
using System.Text;
using System.IO;
using System.Xml;

namespace VNF.Portal.Controllers
{
    public class J1B1N_CFOPController : Controller
    {
        //
        // GET: /J1B1N_Material/

        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("J1B1", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|J1B1";

            GetData();
            return View();

        }

        public DataTable Atualizar()
        {

            BLJ1B1N_CFOP objBLJ1B1N_CFOP = new BLJ1B1N_CFOP();
            DataTable dt = objBLJ1B1N_CFOP.GetAll();

            return dt;
        }

        public PartialViewResult GetData()
        {

            BLJ1B1N_CFOP objBLJ1B1N_CFOP = new BLJ1B1N_CFOP();
            DataTable dt = objBLJ1B1N_CFOP.GetAll();
            return PartialView("Grid", dt);
        }

        public string Salvar(string pCFOP, string pDescricao)
        {
            BLJ1B1N_CFOP objBLJ1B1N_CFOP = new BLJ1B1N_CFOP();
            objBLJ1B1N_CFOP.Adicionar(pCFOP, pDescricao);


            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Editar(string pCFOP, string pDescricao)
        {
            BLJ1B1N_CFOP objBLJ1B1N_CFOP = new BLJ1B1N_CFOP();
            objBLJ1B1N_CFOP.Editar(pCFOP, pDescricao);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

        public string Excluir(string pCFOP)
        {
            BLJ1B1N_CFOP objBLJ1B1N_CFOP = new BLJ1B1N_CFOP();
            objBLJ1B1N_CFOP.Excluir(pCFOP);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                        { "result", "Ok" },
                        { "mensagem", "Material realizada com sucesso"}
                    });

        }

    }
}
