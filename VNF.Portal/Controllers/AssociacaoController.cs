using System;
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
using MetsoFramework.SAP;
using System.Collections;
using VNF.Portal.Models;



namespace VNF.Portal.Controllers
{
    public class AssociacaoController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("ANFE", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|ANFE";

            DataTable dt = LoadData();
            return View(dt);
        }

        [HttpGet]
        public DataTable LoadData()
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();

            DataTable dt = objBLNotaFiscal.GetPendentes();
            return dt;
        }

        [HttpGet]
        public ActionResult Edit(string id = "")
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            modVerificar objVerificar = new modVerificar();
            modNF objNF = objBLNotaFiscal.GetByID(id, false);
            ViewBag.StatusIntegracao = objBLNotaFiscal.GetStatusIntegracao(objNF.VNF_CHAVE_ACESSO);
            ViewBag.id = id;
            ViewBag.Modificar = objBLNotaFiscal.PodeModificar(objNF.VNF_CHAVE_ACESSO);

            ViewBag.IsMandatoryNotaFiscalReferenciada = false;
            if (objNF.ITENS_NF.FirstOrDefault() != null)
            {
                ViewBag.IsMandatoryNotaFiscalReferenciada = objVerificar.IsMandatoryNotaFiscalReferenciada(objNF.ITENS_NF.FirstOrDefault().NF_PROD_CFOP);
            }

            ViewBag.IsFaturaConsignacao = false;
            if (objNF.ITENS_NF.FirstOrDefault() != null)
            {
                ViewBag.IsFaturaConsignacao = objVerificar.IsFaturaEntregaFutOUConsignacao(objNF.ITENS_NF.FirstOrDefault().NF_PROD_CFOP, "ID_MODO_PROCESSO_FATURA_CONSIGNACAO", objNF.NF_IDE_FINNFE);
            }

            ViewBag.IsSubContratacao = false;
            if (objNF.ITENS_NF.Where(x => x.VNF_IS_SUBCONTRATACAO == true).Count() > 0)
            {
                ViewBag.IsSubContratacao = true;
            }


            ViewBag.IsNotaComplementar = false;
            if (objNF.NF_IDE_FINNFE == 2)
            {
                ViewBag.IsNotaComplementar = true;
            }

            return View(objNF);
        }

        [HttpGet]
        public ActionResult Associar(string id)
        {
            BLNotaFiscal oBLNotaFiscal = new BLNotaFiscal();
            string strMensagem = "";
            modNF objNF = oBLNotaFiscal.Validar(id, string.Empty, Uteis.LogonName(), ref strMensagem);
            ViewBag.MensagemSap = strMensagem;
            return View("Edit", objNF);
        }

        [HttpGet]
        public ActionResult GetXml(string id)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string dadosRetorno = objBLNotaFiscal.GetXml(id);

            //string dadosRetorno = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\" versao=\"2.00\"><NFe xmlns=\"http://www.portalfiscal.inf.br/nfe\"><infNFe Id=\"NFe35150100988202000193550010000171341932337988\" versao=\"2.00\"><ide><cUF>35</cUF><cNF>93233798</cNF><natOp>Vendas</natOp><indPag>1</indPag><mod>55</mod><serie>1</serie><nNF>17134</nNF><dEmi>2015-01-05</dEmi><tpNF>1</tpNF><cMunFG>3552205</cMunFG><tpImp>2</tpImp><tpEmis>1</tpEmis><cDV>8</cDV><tpAmb>1</tpAmb><finNFe>1</finNFe><procEmi>0</procEmi><verProc>AGIW</verProc></ide><emit><CNPJ>00988202000193</CNPJ><xNome>CONECT PECAS E ACESSORIOS INDUSTRIAIS EIRELLI</xNome><xFant>CONECT</xFant><enderEmit><xLgr>AV. FERNANDO STECCA</xLgr><nro>423</nro><xBairro>VILA SAO JUDAS</xBairro><cMun>3552205</cMun><xMun>Sorocaba</xMun><UF>SP</UF><CEP>18087149</CEP><cPais>1058</cPais><xPais>BRASIL</xPais><fone>1532384711</fone></enderEmit><IE>669308700118</IE><CRT>1</CRT></emit><dest><CNPJ>16622284000198</CNPJ><xNome>METSO BRASIL INDUSTRIA E COMERCIO LTDA.</xNome><enderDest><xLgr>AV. INDEPENDENCIA</xLgr><nro>2500</nro><xBairro>Iporanga</xBairro><cMun>3552205</cMun><xMun>Sorocaba</xMun><UF>SP</UF><CEP>18087101</CEP><cPais>1058</cPais><xPais>BRASIL</xPais><fone>1521021300</fone></enderDest><IE>669575300114</IE><email>nfefornecedor.mctbr@metso.com</email></dest><det nItem=\"1\"><prod><cProd>1001985750</cProd><cEAN></cEAN><xProd>TEE FEMEA GIR LATERAL X MACHO JIC 37  3/4 8 R6X-S</xProd><NCM>73072200</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>20.0000</qCom><vUnCom>45.6100</vUnCom><vProd>912.20</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>20.0000</qTrib><vUnTrib>45.6100</vUnTrib><indTot>1</indTot><xPed>4501269058</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>35.12</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501269058;</infAdProd></det><det nItem=\"2\"><prod><cProd>MM0349200</cProd><cEAN></cEAN><xProd>PORCA ACO M12X1,5 M06LCFX</xProd><NCM>73072900</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>4.0000</qCom><vUnCom>1.9500</vUnCom><vProd>7.80</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>4.0000</qTrib><vUnTrib>1.9500</vUnTrib><indTot>1</indTot><xPed>4501280945</xPed><nItemPed>000050</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>0.30</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501280945;</infAdProd></det><det nItem=\"3\"><prod><cProd>53334267006</cProd><cEAN></cEAN><xProd>MANGUEIRA C/ TERMINAIS PONTA LISA C/ 4000MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>5.0000</qCom><vUnCom>101.1300</vUnCom><vProd>505.65</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>5.0000</qTrib><vUnTrib>101.1300</vUnTrib><indTot>1</indTot><xPed>4501280945</xPed><nItemPed>000060</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>19.47</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501280945;</infAdProd></det><det nItem=\"4\"><prod><cProd>53334267006</cProd><cEAN></cEAN><xProd>MANGUEIRA C/ TERMINAIS PONTA LISA C/ 4000MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>1.0000</qCom><vUnCom>101.1400</vUnCom><vProd>101.14</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>1.0000</qTrib><vUnTrib>101.1400</vUnTrib><indTot>1</indTot><xPed>4501291879</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>3.89</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501291879;</infAdProd></det><det nItem=\"5\"><prod><cProd>1044252204</cProd><cEAN></cEAN><xProd>MANGUEIRA F471TC6868080806-775MM</xProd><NCM>40092110</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>68.0000</qCom><vUnCom>63.7300</vUnCom><vProd>4333.64</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>68.0000</qTrib><vUnTrib>63.7300</vUnTrib><indTot>1</indTot><xPed>4501295340</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>166.85</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501295340;</infAdProd></det><det nItem=\"6\"><prod><cProd>MM0217667</cProd><cEAN></cEAN><xProd>BUJAO SEXTAVADO INTERNO 1/2 BSP INOX VSTI1/2ED71</xProd><NCM>73072200</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>1.0000</qCom><vUnCom>48.9400</vUnCom><vProd>48.94</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>1.0000</qTrib><vUnTrib>48.9400</vUnTrib><indTot>1</indTot><xPed>4501295340</xPed><nItemPed>000050</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>1.88</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501295340;</infAdProd></det><det nItem=\"7\"><prod><cProd>1044252612</cProd><cEAN></cEAN><xProd>MANGUEIRA F471TC6868080806-2175MM</xProd><NCM>40092190</NCM><CFOP>5102</CFOP><uCom>PC</uCom><qCom>8.0000</qCom><vUnCom>99.2300</vUnCom><vProd>793.84</vProd><cEANTrib></cEANTrib><uTrib>PC</uTrib><qTrib>8.0000</qTrib><vUnTrib>99.2300</vUnTrib><indTot>1</indTot><xPed>4501297913</xPed><nItemPed>000010</nItemPed></prod><imposto><ICMS><ICMSSN101><orig>0</orig><CSOSN>101</CSOSN><pCredSN>3.85</pCredSN><vCredICMSSN>30.56</vCredICMSSN></ICMSSN101></ICMS><PIS><PISOutr><CST>99</CST><vBC>0</vBC><pPIS>0.00</pPIS><vPIS>0.00</vPIS></PISOutr></PIS><COFINS><COFINSOutr><CST>99</CST><vBC>0</vBC><pCOFINS>0.00</pCOFINS><vCOFINS>0.00</vCOFINS></COFINSOutr></COFINS></imposto><infAdProd>PEDIDO 4501297913;</infAdProd></det><total><ICMSTot><vBC>0.00</vBC><vICMS>0.00</vICMS><vBCST>0.00</vBCST><vST>0.00</vST><vProd>6703.21</vProd><vFrete>0.00</vFrete><vSeg>0.00</vSeg><vDesc>0.00</vDesc><vII>0</vII><vIPI>0.00</vIPI><vPIS>0.00</vPIS><vCOFINS>0.00</vCOFINS><vOutro>0.00</vOutro><vNF>6703.21</vNF></ICMSTot></total><transp><modFrete>1</modFrete><transporta><CNPJ>52548435014200</CNPJ><xNome>JSL S/A</xNome><IE>669653462115</IE><xEnder>AVENIDA JEROME CASE 2302</xEnder><xMun>Sorocaba</xMun><UF>SP</UF></transporta><vol><qVol>1</qVol><esp>VOLUME</esp></vol></transp><cobr><dup><nDup>017134A</nDup><dVenc>2015-02-16</dVenc><vDup>6703.21</vDup></dup></cobr><infAdic><infCpl>Empresa optante pelo simples nacional conforme L.C 123/2006 - ;Permite o aproveitamento de credito de ICMS conforme aliquota 3,85% no valor de R$: 258,07</infCpl></infAdic></infNFe><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"#NFe35150100988202000193550010000171341932337988\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /><Transform Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>Qab3OPDblmrAfISyM+FqgBIdVaw=</DigestValue></Reference></SignedInfo><SignatureValue>hLzGsismfcllwXp/O9b78ifzwlMDDOgDgmEVusDKwx8Wh1urbji58J/9+XX0i/hATqnybzCTO6r+f9LoSkcohtNkuE/+xQ0a4UR+pXGUFIw7SPhRobwh9LVGQPHalF06uu5gbb1LI0i5Pt/mokCT2rNrgN5hN93GT5q15USrLZdM+PZf2o6uqelpgAvachCZl7yDc4RtPC6XVlMUzvXBQQ42s8lZlpXdwLW2vksaX624naw8eAo8glCqDDSktvettqtz5MJahSs0ZhqQI8wLVp8NEM7xKHKJIhWgFmT3sdriHC6rzZXiHgygtfq5aZ1ebS1W+v929kR1B2cUFBQzng==</SignatureValue><KeyInfo><X509Data><X509Certificate>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</X509Certificate></X509Data></KeyInfo></Signature></NFe><protNFe versao=\"2.00\" xmlns=\"http://www.portalfiscal.inf.br/nfe\"><infProt><tpAmb>1</tpAmb><verAplic>SP_NFE_PL_006q</verAplic><chNFe>35150100988202000193550010000171341932337988</chNFe><dhRecbto>2015-01-05T17:22:10</dhRecbto><nProt>135150004276546</nProt><digVal>Qab3OPDblmrAfISyM+FqgBIdVaw=</digVal><cStat>100</cStat><xMotivo>Autorizado o uso da NF-e</xMotivo></infProt></protNFe></nfeProc>";
            dadosRetorno = dadosRetorno.Replace("\"", "'");

            string fileName = id + ".xml";

            string xmlText = dadosRetorno;

            // Create an UTF8 byte buffer from it (assuming UTF8 is the desired encoding)
            byte[] xmlBuffer = Encoding.UTF8.GetBytes(xmlText);

            // Write the UTF8 byte buffer to the response stream
            Stream stream = Response.OutputStream;
            Response.ContentType = "text/xml";
            Response.ContentEncoding = Encoding.UTF8;
            stream.Write(xmlBuffer, 0, xmlBuffer.Length);

            // Done
            stream.Close();
            Response.End();
            return File(fileName, "application/xml", fileName);
        }

        [HttpGet]
        public string Exportar()
        {

            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            StringBuilder dadosRetorno = new StringBuilder();

            DataTable dt = objBLNotaFiscal.GetPendentes();

            string fileName = "ANFE - " + DateTime.Now.ToString("dd.MM.yyyy HH.mm.ss") + ".xls";
            string filePath = Path.Combine(this.Server.MapPath("~\\Files\\Export"), fileName);

            MetsoFramework.Files.HtmlFile.ExportToExcel(dt, filePath);

            return fileName;
        }

        [HttpGet]
        public string AddPedidoItem(string txtIdNF, string itemNota, string txtPedido, string txtItemPedido)
        {
            if (modSQL.ExecuteScalar("select VNF_TIPO_DOCUMENTO  from TbDOC_CAB where nfeid = '" + txtIdNF + "'") != "TAL")
            {
                BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
                return objBLNotaFiscal.SetPurchasedOrderInfo(txtIdNF, itemNota, txtPedido, txtItemPedido, Uteis.LogonName());
            }
            else
            {
                return string.Empty;
            }
        }

        [HttpGet]
        public string AddNotaReferenciada(string txtIdNF, string txtNumero, string txtSerie)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            return objBLNotaFiscal.SetReferencedOrderInfo(txtIdNF, txtNumero, txtSerie, Uteis.LogonName());
        }

        [HttpGet]
        public string ConsultarPedido(string nfeid, int itemNf)
        {
            string strMensagem = "";
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            modNF objNotaFiscal = objBLNotaFiscal.GetByID(nfeid, true, ref strMensagem);

            if (!String.IsNullOrEmpty(strMensagem))
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Erro" }, { "data", strMensagem } });
            }

            modNFItem objItemNotaFiscal = objNotaFiscal.ITENS_NF[itemNf - 1];
            SAP_RFC.PurchaseOrderItemsTaxes objIpi = new SAP_RFC.PurchaseOrderItemsTaxes();
            if (objItemNotaFiscal.SAP_ITEM_DETAILS.ITEM_TAXES != null)
            {
                foreach (SAP_RFC.PurchaseOrderItemsTaxes objTax in objItemNotaFiscal.SAP_ITEM_DETAILS.ITEM_TAXES)
                {
                    if (objTax.TAX_NAME.ToUpper() == "IPI")
                    {
                        objIpi = objTax;
                        break;
                    }
                }
            }

            SAP_RFC.PurchaseOrderItemsTaxes objIcms = new SAP_RFC.PurchaseOrderItemsTaxes();
            if (objItemNotaFiscal.SAP_ITEM_DETAILS.ITEM_TAXES != null)
            {
                foreach (SAP_RFC.PurchaseOrderItemsTaxes objTax in objItemNotaFiscal.SAP_ITEM_DETAILS.ITEM_TAXES)
                {
                    if (objTax.TAX_NAME.ToUpper() == "ICMS")
                    {
                        objIcms = objTax;
                        break;
                    }
                }
            }

            string strItem = objItemNotaFiscal.SAP_ITEM_DETAILS.ITEM_NUMBER == 0 ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.ITEM_NUMBER.ToString();
            string strMaterial = String.IsNullOrEmpty(objItemNotaFiscal.SAP_ITEM_DETAILS.MATERIAL) ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.MATERIAL;
            string strDescricao = String.IsNullOrEmpty(objItemNotaFiscal.SAP_ITEM_DETAILS.MATERIAL_DESCRIPTION) ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.MATERIAL_DESCRIPTION;
            string strQuantidade = objItemNotaFiscal.SAP_ITEM_DETAILS.PO_QUANTITY == 0 ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.PO_QUANTITY.ToString("N2");
            string strUnidade = String.IsNullOrEmpty(objItemNotaFiscal.SAP_ITEM_DETAILS.UNIT_OF_MEASURE) ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.UNIT_OF_MEASURE;
            string strValor = objItemNotaFiscal.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES == 0 ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.SAP_ITEM_VALUE_WITH_TAXES.ToString("C2");
            string strDeliveryComp = String.IsNullOrEmpty(objItemNotaFiscal.SAP_ITEM_DETAILS.DELIVERY_COMPLETED) ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.DELIVERY_COMPLETED;
            string strDeletion = String.IsNullOrEmpty(objItemNotaFiscal.SAP_ITEM_DETAILS.DELETION_INDICATOR) ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.DELETION_INDICATOR;
            string strIpi = objIpi.TAX_AMOUNT.ToString("C2");
            string strIcms = objIcms.TAX_AMOUNT.ToString("C2");
            string strControlKey = String.IsNullOrEmpty(objItemNotaFiscal.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY) ? "---" : objItemNotaFiscal.SAP_ITEM_DETAILS.CONFIRMATION_CONTROL_KEY;
            string strConfPortal = objItemNotaFiscal.VNF_CONFIRMADO_PORTAL ? "SIM" : "NÃO";

            string strDadosPedido = @"
                <tr>
                    <td><a data-toggle=""collapse"" data-parent=""#accordion"" href=""#tableone"" class=""collapsed""><i class=""fa fa-chevron-down""></i></a></td>
                    <td>" + strItem + @"</td>
                    <td>" + strMaterial + @"</td>
                    <td>" + strDescricao + @"</td>
                    <td>" + strQuantidade + @"</td>
                    <td>" + strUnidade + @"</td>
                    <td>" + strValor + @"</td>
                    <td>" + strDeliveryComp + @"</td>
                    <td>" + strDeletion + @"</td>
                    <td>" + strIpi + @"</td>
                    <td>" + strIcms + @"</td>
                    <td>" + strControlKey + @"</td>
                    <td>" + strConfPortal + @"</td>
                </tr>
                <tr id=""tableone"" class=""collapse"">
                    <td colspan=""13"">
                        <div>Detalhes</div>
                    </td>
                </tr>
            ";

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                      { "data", strDadosPedido}});
        }

        [HttpGet]
        public string ConsultarInbound(string nfeid, int itemNf)
        {
            string strMensagem = "";
            modNFItem objItemNotaFiscal;
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            modNF objNotaFiscal = objBLNotaFiscal.GetByID(nfeid, true, ref strMensagem);

            if (!String.IsNullOrEmpty(strMensagem))
                return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Erro" }, { "data", strMensagem } });
            else
                objItemNotaFiscal = objNotaFiscal.ITENS_NF.Where(x => x.NF_PROD_ITEM == itemNf).FirstOrDefault();

            string strTemplateInbounds = @"
                                <table id='dttConsultaInbound' datatables_fixedheader='top' datatables_fixedheader_offsettop='60' class='table table-striped table-hover table-bordered' width='100%'>
                                    <thead>
                                        <tr>
                                            <th width='5%'></th>
                                            <th width='5%'>Ativo</th>
                                            <th width='20%'>Inbound</th>
                                            <th width='10%'>Item</th>
                                            <th width='15%'>Entrega</th>
                                            <th width='15%'>Quantidade</th>
                                            <th width='15%'>Alocado</th>
                                            <th width='15%'>Saldo</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        %LINHA_INBOUND%
                                    </tbody>
                                </table>
                               ";

            string strTemplateLinhasInbound = @"
                                        <tr>
                                            <td class='center'><a data-toggle='collapse' href='#detalheItem_%INDICE%' class='collapsed'><i id='btnShowNfsInbound_%INDICE%' class='fa fa-chevron-down' onclick='CarregarNotasInbound(%INDICE%)'></i></a></td>
                                            <td id='tdInbound_%INDICE%' class='center selecionarInbound'>%ID_ATIVO%</td>
                                            <td><a href='#' title='Selecionar inbound' onclick=""SelecionarInbound(%INDICE%, '%ID_NUMERO%', %ID_ITEM%)"">%ID_NUMERO%</a></td>
                                            <td>%ID_ITEM%</td>
                                            <td>%ID_ENTREGA%</td>
                                            <td>%ID_QTD%</td>
                                            <td>%ID_ALOCADO%</td>
                                            <td>%ID_SALDO%</td>
                                        </tr>
                                        <tr id='detalheItem_%INDICE%' class='collapse'>%NOTAS_FISCAIS%</tr>
                                      ";

            string strTemplateNotasFiscais = @"
                                        <td colspan='8' class='padding0'>
                                            <table id='dttTabela' class='table table-bordered margin0' width='100%'>
                                                <thead>
                                                    <tr>
                                                        <th width='20%'>DOC-e</th>
                                                        <th width='10%'>Série</th>
                                                        <th width='15%'>Emissão</th>
                                                        <th width='10%'>Item</th>
                                                        <th width='15%'>CFOP</th>
                                                        <th width='15%'>Situacao</th>
                                                        <th width='15%'>Quantidade</th>
                                                    </tr>
                                                </thead>
                                                <tbody>
                                                    %LINHA_NOTA_FISCAL%
                                                </tbody>
                                            </table>
                                        </td>
                                     ";

            string strTemplateLinhasNotasFiscais = @"
                                            <tr>
                                                <td><a href='%NF_LINK%' target='_blank'>%NF_NUMERO%</a></td>
                                                <td>%NF_SERIE%</td>
                                                <td>%NF_EMISSAO%</td>
                                                <td>%NF_ITEM%</td>
                                                <td>%NF_CFOP%</td>
                                                <td>%NF_SITUACAO%</td>
                                                <td>%NF_QTD%</td>
                                            </tr>
                                           ";

            int indice = 0;
            string strInboundAtiva;
            string strLinkNF;
            StringBuilder stbInbounds = new StringBuilder();
            string strNotasFiscais = "";
            StringBuilder stbNotasFiscais = new StringBuilder();
            foreach (modInboundDelivery objInbound in objItemNotaFiscal.VNF_INBOUND)
            {
                stbNotasFiscais = new StringBuilder();
                strInboundAtiva = string.Empty;
                indice++;

                if (objInbound.NOTAS_FISCAIS.Count == 0)
                {
                    strNotasFiscais = "<td colspan='8' class='padding0'><label class='padding5 padding-top7 txt-color-blue'>Nenhuma nota fiscal associada com esta inbound delivery</label></td>";
                }
                else
                {
                    foreach (modInboundDeliveryNFs objNfs in objInbound.NOTAS_FISCAIS)
                    {
                        strLinkNF = this.Url.Action("Edit", "Associacao", new { id = objNfs.VNF_NFEID }, this.Request.Url.Scheme);
                        stbNotasFiscais.AppendLine(strTemplateLinhasNotasFiscais.Replace("%NF_NFEID%", objNfs.VNF_NFEID)
                                                                        .Replace("%NF_NUMERO%", objNfs.VNF_NUMERO)
                                                                        .Replace("%NF_SERIE%", objNfs.VNF_SERIE)
                                                                        .Replace("%NF_EMISSAO%", objNfs.VNF_EMISSAO.ToString("dd/MM/yyyy"))
                                                                        .Replace("%NF_ITEM%", objNfs.VNF_ITEM.ToString())
                                                                        .Replace("%NF_CFOP%", objNfs.VNF_CFOP)
                                                                        .Replace("%NF_SITUACAO%", objNfs.VNF_SITUACAO)
                                                                        .Replace("%NF_QTD%", objNfs.VNF_QTY.ToString("N2"))
                                                                        .Replace("%NF_LINK%", strLinkNF));
                    }
                    strNotasFiscais = strTemplateNotasFiscais.Replace("%LINHA_NOTA_FISCAL%", stbNotasFiscais.ToString());
                }

                if (objItemNotaFiscal.VNF_INBOUND_DELIVERY_NUMBER == objInbound.SAP_INBOUND_DELIVERY_NUMBER && objItemNotaFiscal.VNF_INBOUND_DELIVERY_ITEM_NUMBER == objInbound.SAP_INBOUND_DELIVERY_ITEM_NUMBER)
                    strInboundAtiva = "<i class='fa fa-check txt-color-blue'></i>";

                stbInbounds.AppendLine(strTemplateLinhasInbound.Replace("%INDICE%", indice.ToString())
                                                       .Replace("%ID_ATIVO%", strInboundAtiva)
                                                       .Replace("%ID_NUMERO%", objInbound.SAP_INBOUND_DELIVERY_NUMBER)
                                                       .Replace("%ID_ITEM%", objInbound.SAP_INBOUND_DELIVERY_ITEM_NUMBER.ToString())
                                                       .Replace("%ID_ENTREGA%", objInbound.SAP_DELIVERY_DATE.ToString("dd/MM/yyyy"))
                                                       .Replace("%ID_QTD%", objInbound.SAP_QTY.ToString("N2"))
                                                       .Replace("%ID_ALOCADO%", objInbound.NOTAS_FISCAIS.Sum(x => x.VNF_QTY).ToString("N2"))
                                                       .Replace("%ID_SALDO%", objInbound.OPEN_QTY.ToString("N2"))
                                                       .Replace("%NOTAS_FISCAIS%", strNotasFiscais));
            }

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" },
                                                                                      { "data", strTemplateInbounds.Replace("%LINHA_INBOUND%", stbInbounds.ToString()) }});
        }

        [HttpGet]
        public string SelecionarInbound(string nfeid, int itemNf, string inboundDelivery, int inboundDeliveryItem)
        {
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string strMensagem = objBLNotaFiscal.SelecionarInboundDelivery(nfeid, itemNf, inboundDelivery, inboundDeliveryItem);
            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "result", "Ok" }, { "data", strMensagem } });
        }

        [HttpGet]
        public string GetRefNF(string pStrNFEID, int pStrItemNF)
        {
            try
            {
                DataLayer.DL_TBDOC_ITEM_NFE vObjDLItemNF = new DataLayer.DL_TBDOC_ITEM_NFE();
                ArrayList vObjArrayRefNFItems = new ArrayList();

                var vLstRefNF = vObjDLItemNF.db.TBDOC_CONSIGNACAO_REFNF.Where(x => x.NFEID == pStrNFEID && x.ITEM_NF == pStrItemNF).ToList();


                foreach (var item in vLstRefNF)
                {
                    vObjArrayRefNFItems.Add(new
                    {
                        NUMERO_REFNF = item.NUMERO_REFNF,
                        SERIE_REFNF = item.SERIE_REFNF
                    }
                    );
                }

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                        {"RefNFList", vObjArrayRefNFItems},
                                                                                        {"sucesso", true }
                                                                                                            });
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro: ", ex.Message}});

            }


        }
        [HttpGet]
        public string GetRefNFSubContratacao(string pStrNFEID, int pStrItemNF)
        {
            try
            {
                DataLayer.DL_TBDOC_ITEM_NFE vObjDLItemNF = new DataLayer.DL_TBDOC_ITEM_NFE();
                ArrayList vObjArrayRefNFItems = new ArrayList();

                var vLstRefNF = vObjDLItemNF.db.TBDOC_SUBCONTRATACAO_REFNF.Where(x => x.NFEID == pStrNFEID && x.ITEM_NF == pStrItemNF).ToList();


                foreach (var item in vLstRefNF)
                {
                    vObjArrayRefNFItems.Add(new
                    {
                        NUMERO_REFNF = item.NUMERO_REFNF,
                        SERIE_REFNF = item.SERIE_REFNF,
                        ITEM_REFNF = item.ITEM_REFNF
                    }
                    );
                }

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                        {"RefNFList", vObjArrayRefNFItems},
                                                                                        {"sucesso", true }
                                                                                                            });
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro: ", ex.Message}});

            }


        }
        [HttpGet]
        public string GetRefNFNotaComplemenetar(string pStrNFEID, int pStrItemNF)
        {
            try
            {
                DataLayer.DL_TBDOC_ITEM_NFE vObjDLItemNF = new DataLayer.DL_TBDOC_ITEM_NFE();
                ArrayList vObjArrayRefNFItems = new ArrayList();

                var vLstRefNF = vObjDLItemNF.db.TBDOC_NOTA_COMPLEMENTAR_REFNF.Where(x => x.NFEID == pStrNFEID && x.ITEM_NF == pStrItemNF).ToList();

                if (vLstRefNF.Count > 0)
                {
                    foreach (var item in vLstRefNF)
                    {
                        vObjArrayRefNFItems.Add(new
                        {
                            NUMERO_REFNF = item.NUMERO_REFNF,
                            SERIE_REFNF = item.SERIE_REFNF
                        }
                        );
                    }
                }
                else
                {
                    BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
                    modNF objNF = objBLNotaFiscal.GetByID(pStrNFEID, false);
                    vObjArrayRefNFItems.Add(new
                    {
                        NUMERO_REFNF = objNF.ITENS_NF.Where(x => x.NF_PROD_ITEM == pStrItemNF).Select(x => x.VNF_NFREF_NOTA_COMPLEMENTAR.FirstOrDefault().NUMERO_REFNF),
                        SERIE_REFNF = objNF.ITENS_NF.Where(x => x.NF_PROD_ITEM == pStrItemNF).Select(x => x.VNF_NFREF_NOTA_COMPLEMENTAR.FirstOrDefault().SERIE_REFNF)
                    }
                    );
                }

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                        {"RefNFList", vObjArrayRefNFItems},
                                                                                        {"sucesso", true }
                                                                                                            });
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro: ", ex.Message}});

            }


        }
        [HttpPost]
        public string SalvarRefNF(List<TBDOC_CONSIGNACAO_REFNF> pObjRefNF)
        {
            DataLayer.DLTBDOC_CONSIGNACAO_REFNF vObjDLTBDOC_CONSIGNACAO_REFNF = new DataLayer.DLTBDOC_CONSIGNACAO_REFNF();

            try
            {
                if (pObjRefNF.Count > 0)
                {
                    //removendo notas de referência
                    int vIntItenNF = pObjRefNF[0].ITEM_NF;
                    string vStrNFEID = pObjRefNF[0].NFEID;
                    foreach (var item in vObjDLTBDOC_CONSIGNACAO_REFNF.db.TBDOC_CONSIGNACAO_REFNF.Where(x => x.NFEID == vStrNFEID && x.ITEM_NF == vIntItenNF).ToList())
                    {
                        vObjDLTBDOC_CONSIGNACAO_REFNF.db.TBDOC_CONSIGNACAO_REFNF.Remove(item);
                        vObjDLTBDOC_CONSIGNACAO_REFNF.Save();
                    }

                    //incluindo novas notas de referência
                    foreach (var item in pObjRefNF)
                    {
                        vObjDLTBDOC_CONSIGNACAO_REFNF.Insert(item);
                        vObjDLTBDOC_CONSIGNACAO_REFNF.Save();
                    }

                }

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", true }});
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro",".NetMessage: " + ex.Message}});
                throw;
            }
        }

        [HttpPost]
        public string SalvarRefNFCompelmentar(List<TBDOC_NOTA_COMPLEMENTAR_REFNF> pObjRefNF)
        {
            DataLayer.DLTBDOC_NOTA_COMPLEMENTAR_REFNF vObjDLTBDOC_NOTA_COMPLEMENTAR_REFNF = new DataLayer.DLTBDOC_NOTA_COMPLEMENTAR_REFNF();

            try
            {
                if (pObjRefNF.Count > 0)
                {
                    //removendo notas de referência
                    int vIntItenNF = pObjRefNF[0].ITEM_NF;
                    string vStrNFEID = pObjRefNF[0].NFEID;
                    foreach (var item in vObjDLTBDOC_NOTA_COMPLEMENTAR_REFNF.db.TBDOC_NOTA_COMPLEMENTAR_REFNF.Where(x => x.NFEID == vStrNFEID && x.ITEM_NF == vIntItenNF).ToList())
                    {
                        vObjDLTBDOC_NOTA_COMPLEMENTAR_REFNF.db.TBDOC_NOTA_COMPLEMENTAR_REFNF.Remove(item);
                        vObjDLTBDOC_NOTA_COMPLEMENTAR_REFNF.Save();
                    }

                    //incluindo novas notas de referência
                    foreach (var item in pObjRefNF)
                    {
                        vObjDLTBDOC_NOTA_COMPLEMENTAR_REFNF.Insert(item);
                        vObjDLTBDOC_NOTA_COMPLEMENTAR_REFNF.Save();
                    }

                }

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", true }});
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro",".NetMessage: " + ex.Message}});
                throw;
            }
        }


        [HttpPost]
        public string SalvarRefNFSubContratacao(List<TBDOC_SUBCONTRATACAO_REFNF> pObjRefNF)
        {
            DataLayer.DLTBDOC_SUBCONTRATACAO_REFNF vObjDLTBDOC_SUBCONTRATACAO_REFNF = new DataLayer.DLTBDOC_SUBCONTRATACAO_REFNF();

            try
            {
                if (pObjRefNF.Count > 0)
                {
                    //removendo notas de referência
                    int vIntItenNF = pObjRefNF[0].ITEM_NF;
                    string vStrNFEID = pObjRefNF[0].NFEID;
                    foreach (var item in vObjDLTBDOC_SUBCONTRATACAO_REFNF.db.TBDOC_SUBCONTRATACAO_REFNF.Where(x => x.NFEID == vStrNFEID && x.ITEM_NF == vIntItenNF).ToList())
                    {
                        vObjDLTBDOC_SUBCONTRATACAO_REFNF.db.TBDOC_SUBCONTRATACAO_REFNF.Remove(item);
                        vObjDLTBDOC_SUBCONTRATACAO_REFNF.Save();
                    }

                    //incluindo novas notas de referência
                    foreach (var item in pObjRefNF)
                    {
                        vObjDLTBDOC_SUBCONTRATACAO_REFNF.Insert(item);
                        vObjDLTBDOC_SUBCONTRATACAO_REFNF.Save();
                    }

                }

                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", true }});
            }
            catch (Exception ex)
            {
                return Serialization.JSON.CreateString(new Dictionary<string, object>() {
                                                                                     {"sucesso", false },
                                                                                     {"MensagemErro",".NetMessage: " + ex.Message}});
                throw;
            }
        }

        [HttpGet]
        public string AssociarNF(string txtIdNF)
        {
            string strMensagem = "";
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            modNF objNF = objBLNotaFiscal.Validar(txtIdNF, "", Uteis.LogonName(), ref strMensagem);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "status", objNF.VNF_STATUS },
                                                                                      { "mensagem", strMensagem}});
        }

        [HttpGet]
        public string AlterarRelevancia(string txtIdNF, int txtItemNF, string ItemRelevante)
        {
            string strStatus = "";
            bool BloquearMudanca = false;
            BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
            string strMensagem = objBLNotaFiscal.AlterarRelevancia(txtIdNF, txtItemNF, ItemRelevante, ref strStatus, ref BloquearMudanca);

            return Serialization.JSON.CreateString(new Dictionary<string, object>() { { "status", strStatus },
                                                                                      { "mensagem", strMensagem},
                                                                                      { "bloquear", BloquearMudanca}});

        }
    }
}
