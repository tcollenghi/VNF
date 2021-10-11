using System;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using MetsoFramework.Utils;
using MetsoFramework.Files;
using VNF.Business;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace VNF.Portal.Controllers
{
    public class TriangulusController : Controller
    {

        #region Impersonte

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        // logon types 
        const int LOGON32_LOGON_INTERACTIVE = 2;
        const int LOGON32_LOGON_NETWORK = 3;
        const int LOGON32_LOGON_NEW_CREDENTIALS = 9;

        // logon providers 
        const int LOGON32_PROVIDER_DEFAULT = 0;
        const int LOGON32_PROVIDER_WINNT50 = 3;
        const int LOGON32_PROVIDER_WINNT40 = 2;
        const int LOGON32_PROVIDER_WINNT35 = 1;

        #endregion

        [HttpGet]
        public ActionResult Index()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("EXML", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|EXML";

            DataTable dt = new DataTable();
            dt.Columns.Add("Parametro");
            dt.Columns.Add("Valor");

            return View(dt);
        }

        [HttpGet]
        public ActionResult ProcurarView(string pFile, string id = "")
        {
            XmlDocument doc = new XmlDocument();

            doc.Load(pFile);
            StringWriter sw = new StringWriter();
            XmlTextWriter tx = new XmlTextWriter(sw);
            doc.WriteTo(tx);

            string strMensagem = "";
            string str = sw.ToString();
            modNF mod = new modNF();
            BLNotaFiscal objNF = new BLNotaFiscal();
            mod = objNF.Validar(string.Empty, pFile, Uteis.LogonName(), ref strMensagem);

            return View("ConsultarXML", mod);
        }

        [HttpPost]
        public ActionResult Upload()
        {
            string pFile = string.Empty;
            DataTable dt = new DataTable();
            dt.Columns.Add("Parametro");
            dt.Columns.Add("Valor");
            try
            {
                if (Request.Files.Count > 0)
                {
                    pFile = Path.GetFileName(Request.Files[0].FileName);

                    string nomeArquivo = Server.MapPath("~/Files/Import/") + Uteis.LogonName() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xml";
                    Request.Files[0].SaveAs(nomeArquivo);

                    XmlDocument doc = new XmlDocument();
                    doc.Load(nomeArquivo);

                    StringWriter sw = new StringWriter();
                    XmlTextWriter tx = new XmlTextWriter(sw);
                    doc.WriteTo(tx);

                    //--> COPIA O ARQUIVO PARA A PASTA DO TRIANGULUS
                    IntPtr token = IntPtr.Zero;
                    string user = Uteis.GetSettingsValue<string>("AdUser");
                    string pswd = Uteis.GetSettingsValue<string>("AdPassword");

                    if (!String.IsNullOrEmpty(user))
                        LogonUser(user, "mdir", pswd, LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_DEFAULT, ref token);

                    using (WindowsIdentity.Impersonate(token))
                    {
                        StreamWriter objWriter = new StreamWriter(Uteis.GetSettingsValue<string>("PastaImportacaoTriangulus") + "XML_VNF_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xml");
                        objWriter.Write(doc.InnerXml);
                        objWriter.Close();
                        objWriter.Dispose();
                    }

                    //'--> CARREGA O XML PARA MEMÓRIA
                    modVerificar objModVerificar = new modVerificar();
                    var _objNF = objModVerificar.ParseModNF(nomeArquivo);

                    //--> SE O ARQUIVO FOI ENVIADO PARA O TRIANGULUS, FAZ A LEITURA
                    //string strMensagem = "";
                    //string str = sw.ToString();
                    //modNF objNF = new modNF();
                    //BLNotaFiscal objBLNotaFiscal = new BLNotaFiscal();
                    //objNF = objBLNotaFiscal.Validar(string.Empty, nomeArquivo, Uteis.LogonName(), ref strMensagem);

                    //BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.EnvioXml, "Enviou a " + objBLNotaFiscal.GetNumeroDocumento(objNF.VNF_CHAVE_ACESSO) + " manualmente para o sistema.", objNF.VNF_CHAVE_ACESSO);

                    //string arquivo = nomeArquivo.Substring(nomeArquivo.LastIndexOf("\\") + 1);

                    //BLLog.Insert(BLLog.LogType.Application, Uteis.LogonName(), BLLog.LogTitle.EnvioXml, "Enviou a " + _objNF.VNF_CHAVE_ACESSO + " manualmente para o sistema.", _objNF.VNF_CHAVE_ACESSO);

                    DataRow dr = dt.NewRow();
                    dr["Parametro"] = "Nome do arquivo";
                    dr["Valor"] = pFile;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Chave de Acesso";
                    dr["Valor"] = _objNF.VNF_CHAVE_ACESSO;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Tipo de documento";
                    dr["Valor"] = _objNF.VNF_TIPO_DOCUMENTO;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Número";
                    dr["Valor"] = _objNF.NF_IDE_NNF;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Série";
                    dr["Valor"] = _objNF.NF_IDE_SERIE;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Versão";
                    dr["Valor"] = _objNF.NF_OUTROS_VERSAO;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Status";
                    dr["Valor"] = _objNF.NF_OUTROS_STATUS_CODE + " - " + _objNF.NF_OUTROS_STATUS_DESC;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Situação";
                    dr["Valor"] = _objNF.VNF_STATUS;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Data de Emissão";
                    dr["Valor"] = _objNF.NF_IDE_DHEMI.ToShortDateString();
                    dt.Rows.Add(dr);

                    for (int i = 0; i < _objNF.DUPLICATAS.Count; i++)
                    {
                        dr = dt.NewRow();
                        dr["Parametro"] = "Duplicata " + (i + 1).ToString().PadLeft(2, '0') + " - Número";
                        dr["Valor"] = _objNF.DUPLICATAS[i].NF_COBR_DUP_NDUP;
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Parametro"] = "Duplicata " + (i + 1).ToString().PadLeft(2, '0') + " - Vencimento";
                        dr["Valor"] = _objNF.DUPLICATAS[i].NF_COBR_DUP_DVENC.ToShortDateString();
                        dt.Rows.Add(dr);

                        dr = dt.NewRow();
                        dr["Parametro"] = "Duplicata " + (i + 1).ToString().PadLeft(2, '0') + " - Valor";
                        dr["Valor"] = _objNF.DUPLICATAS[i].NF_COBR_DUP_VDUP.ToString("C2");
                        dt.Rows.Add(dr);
                    }

                    dr = dt.NewRow();
                    dr["Parametro"] = "Nome do Emitente";
                    dr["Valor"] = _objNF.NF_EMIT_XNOME;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "CNPJ do Emitente";
                    dr["Valor"] = _objNF.NF_EMIT_CNPJ.ToCnpj();
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Nome do Destinatário";
                    dr["Valor"] = _objNF.NF_DEST_XNOME;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "CNPJ do Destinatário";
                    dr["Valor"] = _objNF.NF_DEST_CNPJ.ToCnpj();
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Observações Nota Fiscal";
                    dr["Valor"] = _objNF.NF_OUTROS_INFORMACAO_ADICIONAL;
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Assinatura digital";
                    dr["Valor"] = _objNF.NF_OUTROS_SIGNATURE ? "Sim" : "Não";
                    dt.Rows.Add(dr);

                    dr = dt.NewRow();
                    dr["Parametro"] = "Quantidade de itens";
                    if (_objNF.ITENS_NF != null)
                        dr["Valor"] = _objNF.ITENS_NF.Count.ToString() + " ITEM(S)";
                    else
                        dr["Valor"] = "0 ITEM(S)";
                    dt.Rows.Add(dr);

                    ViewBag.id = _objNF.VNF_CHAVE_ACESSO;
                }
            }
            catch (Exception ex)
            {
                DataRow dr = dt.NewRow();
                dr["Parametro"] = "ERRO DURANTE UPLOAD DO ARQUIVO";
                dr["Valor"] = ex.Message;
                dt.Rows.Add(dr);
            }

            return View("Index", dt);
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

        //Marcio Spinosa - 05/06/2019 - CR00009165
        [HttpGet]
        public ActionResult NFCancel()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("EXML", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|EXML";

            DataTable dt = new DataTable();
            dt.Columns.Add("IdNFe");
            //dt.Columns.Add("Valor");
            ViewBag.SelectFiliais = SelectFiliais();
            return View(dt);
        }

        [HttpGet]
        public ActionResult NFChangeStatus()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("EXML", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|EXML";

            //DataTable dt = new DataTable();
            //dt.Columns.Add("IdNFe");
            ////dt.Columns.Add("Valor");
            //ViewBag.SelectFiliais = SelectFiliais();
            return View();
        }

        [HttpGet]
        public ActionResult AddReferenceNF()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("EXML", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|EXML";

            DataTable dt = new DataTable();
            dt.Columns.Add("IdNFe");
            //dt.Columns.Add("Valor");
            ViewBag.SelectFiliais = SelectFiliais();
            return View();
        }

        [HttpGet]
        public ActionResult ICMSSTBaseReduzida()
        {
            BLAcessos objAcesso = new BLAcessos();
            if (objAcesso.ConsultaAcesso("EXML", Uteis.LogonName()) == false)
                ViewBag.Acesso = "false|EXML";

            DataTable dt = new DataTable();
            dt.Columns.Add("IdNFe");
            //dt.Columns.Add("Valor");
            ViewBag.SelectFiliais = SelectFiliais();
            return View();
        }

        private List<SelectListItem> SelectFiliais()
        {
            List<SelectListItem> lista = new List<SelectListItem>();
            BLTriangulus objBLTriangulus = new BLTriangulus();
            DataTable dt = objBLTriangulus.getFilial(modSQL.connectionStringTriangulus);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                lista.Add(new SelectListItem { Value = dt.Rows[i][0].ToString(), Text = dt.Rows[i][1].ToString() });
            }
            return lista;
        }

        [HttpPost]
        public JsonResult validar(string pstrNF, string pstrData, string pstrFilial)
        {
            string pstrResult;
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            string filiais = new BLParametros().GetByParametro("CANCELAMENTO_24_HORAS");
            try
            {

                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.Nfeid = pstrNF;

                if (!string.IsNullOrEmpty(pstrData) && pstrData.Contains("/"))
                    objNF.DTEmissao = Convert.ToDateTime(pstrData);
                else
                {
                    string result = "o campo data não está preenchido corretamente.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                if (!string.IsNullOrEmpty(pstrFilial))
                    objNF.IDFilial = Convert.ToInt32(pstrFilial);
                else
                {
                    string result = "Favor selecionar uma filial.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                pstrResult = new BLTriangulus().verificaRetornoNF(modSQL.ConnectionStringTriangulusCancelamento, objNF, filiais);

                if (pstrResult == "Cancelar")
                {
                    string mensagem = "Deseja cancelar a NF da Filial = " + objNF.IDFilial.ToString() + ", Nota Fiscal = " + objNF.NFNumero.ToString() + ", serie  = " + objNF.Serie + "?";
                    return Json(new { result = "cancelar", data = mensagem });
                }
                else if (pstrResult == "não cancelar")
                {
                    string mensagem = "Não é possível efetuar o cancelamento da nota para a seguinte Filial :" + objNF.IDFilial.ToString();
                    return Json(new { result = "Error", data = mensagem });
                }
                else if (pstrResult == "data")
                {
                    string mensagem = "Não é possível efetuar o ação, o período da nota permite cancelamento via SAP";
                    return Json(new { result = "Error", data = mensagem });
                }
                else if (pstrResult == "data_excedida")
                {
                    string mensagem = "Prazo de cancelamento superior ao previsto na legislação";
                    return Json(new { result = "Error", data = mensagem });
                }
                else if (pstrResult == "Inutilizar")
                {
                    //string mensagem = "Deseja inutilizar o número " + objNF.NFNumero.ToString() + "?";
                    string mensagem = "Deseja inutilizar a NF da Filial = " + objNF.IDFilial.ToString() + ", Nota Fiscal = " + objNF.NFNumero.ToString() + ", serie  = " + objNF.Serie + "?";
                    return Json(new { result = "cancelar", data = mensagem });
                }
                else if (pstrResult == "24_horas")
                {
                    string mensagem = "Não é possível efetuar o cancelamento da nota, para o cancelamento dessa nota é necessário utilizar o SAP em um prazo de 24 horas após a emissão.";
                    return Json(new { result = "Error", data = mensagem });
                }
                else
                    return Json(new { result = "Error", data = "Não foi possível completar a ação, favor revisar as informações." });
            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Cancelar(string pstrNF, string pstrSerie, string pstrData, string pstrFilial)
        {
            int pstrResult;
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            try
            {
                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.NFNumero = Convert.ToInt32(pstrNF);

                if (!string.IsNullOrEmpty(pstrSerie))
                    objNF.Serie = pstrSerie.PadLeft(3, '0');

                if (!string.IsNullOrEmpty(pstrData) && pstrData.Contains("/"))
                    objNF.DTEmissao = Convert.ToDateTime(pstrData);
                else
                {
                    string result = "o campo data não está preenchido corretamente.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                if (!string.IsNullOrEmpty(pstrFilial))
                    objNF.IDFilial = Convert.ToInt32(pstrFilial);
                else
                {
                    string result = "Favor selecionar uma filial.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                pstrResult = new BLTriangulus().CancelarNF(modSQL.ConnectionStringTriangulusCancelamento, objNF, Uteis.LogonName());

                if (pstrResult == 0)
                {
                    string mensagem = "O cancelamento foi efetuado com sucesso";
                    return Json(new { result = "cancelar", data = mensagem });
                }
                else
                    return Json(new { result = "Error", data = "Não foi possível efetuar o cancelamento" });
            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult CancelarNFE(string pstrNF, string pstrData, string pstrFilial)
        {
            int pstrResult;
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            try
            {
                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.Nfeid = pstrNF;

                if (!string.IsNullOrEmpty(pstrData) && pstrData.Contains("/"))
                    objNF.DTEmissao = Convert.ToDateTime(pstrData);
                else
                {
                    string result = "o campo data não está preenchido corretamente.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                if (!string.IsNullOrEmpty(pstrFilial))
                    objNF.IDFilial = Convert.ToInt32(pstrFilial);
                else
                {
                    string result = "Favor selecionar uma filial.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                pstrResult = new BLTriangulus().CancelarChave(modSQL.ConnectionStringTriangulusCancelamento, objNF, Uteis.LogonName());
                if (pstrResult == 0)
                {
                    string mensagem = "O cancelamento foi efetuado com sucesso";
                    return Json(new { result = "ok", data = mensagem });
                }
                else
                    return Json(new { result = "Error", data = "Não foi possível efetuar o cancelamento" });
            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult validarInutilizacao(string pstrFilial, string pnumeroNFe, string pstrSerie, string pstrData)
        {
            string pstrResult;
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            string filiais = new BLParametros().GetByParametro("CANCELAMENTO_24_HORAS");
            try
            {

                if (!string.IsNullOrEmpty(pnumeroNFe))
                    objNF.NFNumero = Convert.ToInt32(pnumeroNFe);

                if (!string.IsNullOrEmpty(pstrSerie))
                    objNF.Serie = pstrSerie.PadLeft(3, '0');
                else
                {
                    string result = "É necessário informar o número de série da nota.";
                    return Json(new { result = "Error", data = result.ToString() });
                }


                if (!string.IsNullOrEmpty(pstrData) && pstrData.Contains("/"))
                    objNF.DTEmissao = Convert.ToDateTime(pstrData);
                else
                {
                    string result = "o campo data não está preenchido corretamente.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                if (!string.IsNullOrEmpty(pstrFilial))
                    objNF.IDFilial = Convert.ToInt32(pstrFilial);
                else
                {
                    string result = "Favor selecionar uma filial.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                pstrResult = new BLTriangulus().verificaRetornoNF(modSQL.ConnectionStringTriangulusCancelamento, objNF, filiais);

                if (pstrResult == "Cancelar")
                {
                    string mensagem = "Deseja cancelar a NF da Filial = " + objNF.IDFilial.ToString() + ", Nota Fiscal = " + objNF.NFNumero.ToString() + ", serie  = " + objNF.Serie + "?";
                    return Json(new { result = "cancelar", data = mensagem });
                }
                else if (pstrResult == "não cancelar")
                {
                    string mensagem = "Não é possível efetuar o cancelamento da nota para a seguinte Filial :" + objNF.IDFilial.ToString();
                    return Json(new { result = "Error", data = mensagem });
                }
                else if (pstrResult == "data")
                {
                    string mensagem = "Não é possível efetuar o ação, o período da nota permite cancelamento via SAP";
                    return Json(new { result = "Error", data = mensagem });
                }
                else if (pstrResult == "data_excedida")
                {
                    string mensagem = "Prazo de cancelamento superior ao previsto na legislação";
                    return Json(new { result = "Error", data = mensagem });
                }
                else if (pstrResult == "Inutilizar")
                {
                    //string mensagem = "Deseja inutilizar o número " + objNF.NFNumero.ToString() + "?";
                    string mensagem = "Deseja inutilizar a NF da Filial = " + objNF.IDFilial.ToString() + ", Nota Fiscal = " + objNF.NFNumero.ToString() + ", serie  = " + objNF.Serie + "?";
                    return Json(new { result = "cancelar", data = mensagem });
                }
                else if (pstrResult == "24_horas")
                {
                    string mensagem = "Não é possível efetuar o cancelamento da nota, para o cancelamento dessa nota é necessário utilizar o SAP em um prazo de 24 horas após a emissão.";
                    return Json(new { result = "Error", data = mensagem });
                }
                else
                    return Json(new { result = "Error", data = "Não foi possível completar a ação, favor revisar as informações." });
            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult validarAlterarStatus(string pstrNF, string pstrStatus)
        {
            string pstrResult;
            string statusvalidos = "100, 101, 155";
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            //string filiais = new BLParametros().GetByParametro("CANCELAMENTO_24_HORAS");
            try
            {

                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.Nfeid = pstrNF;
                else
                {
                    string result = "Favor inserir a chave de acesso da nota a ser corrigida";
                    return Json(new { result = "Error", data = result.ToString() });
                }


                if (!string.IsNullOrEmpty(pstrStatus))
                {
                    if (statusvalidos.Contains(pstrStatus))
                        objNF.Status = pstrStatus;
                    else
                    {
                        string result = "A alteração não pode ser efetuada com o status informado.";
                        return Json(new { result = "Error", data = result.ToString() });
                    }
                }
                else
                {
                    string result = "O Status é obrigatório";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                string mensagem = "Deseja alterar o status ?";
                return Json(new { result = "cancelar", data = mensagem });

            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult validarDadosNotaReferenciada(string pstrFilial, string pstrNF, string pstrSerie, string pstrChaveAcesso)
        {
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            //string filiais = new BLParametros().GetByParametro("CANCELAMENTO_24_HORAS");
            try
            {

                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.NFNumero = Convert.ToInt32(pstrNF);
                else
                {
                    string result = "É necessário no número da nota a qual irá inserir a referência.";
                    return Json(new { result = "Error", data = result.ToString() });
                }


                if (!string.IsNullOrEmpty(pstrFilial))
                    objNF.IDFilial = Convert.ToInt32(pstrFilial);
                else
                {
                    string result = "A filial é obrigatório";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                if (!string.IsNullOrEmpty(pstrSerie))
                    objNF.Serie = pstrSerie.PadLeft(3, '0'); 
                else
                {
                    string result = "É necessário a série da nota fiscal";
                    return Json(new { result = "Error", data = result.ToString() });

                }

                if (!string.IsNullOrEmpty(pstrChaveAcesso))
                    objNF.Nfeid = pstrChaveAcesso;
                else
                {
                    string result = "Favor inserir a chave da nota referenciada";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                string mensagem = "Deseja inserir a referência a nota = " + pstrNF + " série = " + pstrSerie + " para a filial = " + pstrFilial + " ?";
                return Json(new { result = "cancelar", data = mensagem });

            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult validarICMSBaseReduzida(string pstrFilial, string pstrNF, string pstrSerie, string pstrItem)
        {
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            //string filiais = new BLParametros().GetByParametro("CANCELAMENTO_24_HORAS");
            try
            {

                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.NFNumero = Convert.ToInt32(pstrNF);
                else
                {
                    string result = "É necessário no número da nota a qual irá inserir a referência.";
                    return Json(new { result = "Error", data = result.ToString() });
                }


                if (!string.IsNullOrEmpty(pstrFilial))
                    objNF.IDFilial = Convert.ToInt32(pstrFilial);
                else
                {
                    string result = "A filial é obrigatório";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                if (!string.IsNullOrEmpty(pstrSerie))
                    objNF.Serie = pstrSerie.PadLeft(3, '0');
                else
                {
                    string result = "É necessário a série da nota fiscal";
                    return Json(new { result = "Error", data = result.ToString() });

                }

                if (!string.IsNullOrEmpty(pstrItem))
                    objNF.Item = Convert.ToInt32(pstrItem);
                else
                {
                    string result = "É necessário informar o item";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                string mensagem = "Deseja alterar o ICMS ST da nota = " + pstrNF + " série = " + pstrSerie + " para a filial = " + pstrFilial + " ?";
                return Json(new { result = "cancelar", data = mensagem });

            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult AlterarStatus(string pstrNF, string pstrStatus)
        {
            int pstrResult;
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            try
            {
                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.Nfeid = pstrNF;

                if (!string.IsNullOrEmpty(pstrStatus))
                    objNF.Status = pstrStatus;
                else
                {
                    string result = "o campo data não está preenchido corretamente.";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                pstrResult = new BLTriangulus().AlterarStatusNF(modSQL.ConnectionStringTriangulusCancelamento, objNF, Uteis.LogonName());
                if (pstrResult == 0)
                {
                    string mensagem = "A alteração foi efetuado com sucesso.";
                    return Json(new { result = "ok", data = mensagem });
                }
                else
                    return Json(new { result = "Error", data = "Não foi possível alterar o status " });
            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult InserirDadosNotaReferenciada(string pstrFilial, string pstrNF, string pstrSerie, string pstrChaveAcesso)
        {
            int pstrResult;
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            try
            {
                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.NFNumero = Convert.ToInt32(pstrNF);
                else
                {
                    string result = "É necessário no número da nota a qual irá inserir a referência.";
                    return Json(new { result = "Error", data = result.ToString() });
                }


                if (!string.IsNullOrEmpty(pstrFilial))
                    objNF.IDFilial = Convert.ToInt32(pstrFilial);
                else
                {
                    string result = "A filial é obrigatório";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                if (!string.IsNullOrEmpty(pstrSerie))
                    objNF.Serie = pstrSerie.PadLeft(3, '0'); 
                else
                {
                    string result = "É necessário a série da nota fiscal";
                    return Json(new { result = "Error", data = result.ToString() });

                }

                if (!string.IsNullOrEmpty(pstrChaveAcesso))
                    objNF.Nfeid = pstrChaveAcesso;
                else
                {
                    string result = "É necessário a série da nota fiscal";
                    return Json(new { result = "Error", data = result.ToString() });
                }


                pstrResult = new BLTriangulus().InserirNotaReferenciada(modSQL.ConnectionStringTriangulusCancelamento, objNF, Uteis.LogonName());
                if (pstrResult == 0)
                {
                    string mensagem = "A chave " + objNF.Nfeid+ " foi referenciada na nota " + objNF.NFNumero + ".";
                    return Json(new { result = "ok", data = mensagem });
                }
                else
                    return Json(new { result = "Error", data = "Não foi possível alterar o status " });
            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult InserirICMSBaseReduzida(string pstrFilial, string pstrNF, string pstrSerie, string pstrItem)
        {
            int pstrResult;
            NFSaidaTriangulus objNF = new NFSaidaTriangulus();
            objNF.Ambiente = ConfigurationSettings.AppSettings["Ambiente"].ToString();
            objNF.Versao = new BLParametros().GetByParametro("VERSAO_NF");
            try
            {
                if (!string.IsNullOrEmpty(pstrNF))
                    objNF.NFNumero = Convert.ToInt32(pstrNF);
                else
                {
                    string result = "É necessário no número da nota.";
                    return Json(new { result = "Error", data = result.ToString() });
                }


                if (!string.IsNullOrEmpty(pstrFilial))
                    objNF.IDFilial = Convert.ToInt32(pstrFilial);
                else
                {
                    string result = "A filial é obrigatório";
                    return Json(new { result = "Error", data = result.ToString() });
                }

                if (!string.IsNullOrEmpty(pstrSerie))
                    objNF.Serie = pstrSerie.PadLeft(3, '0'); 
                else
                {
                    string result = "É necessário a série da nota fiscal";
                    return Json(new { result = "Error", data = result.ToString() });

                }

                if (!string.IsNullOrEmpty(pstrItem))
                    objNF.Item = Convert.ToInt32(pstrItem);
                else
                {
                    string result = "É necessário a série da nota fiscal";
                    return Json(new { result = "Error", data = result.ToString() });
                }


                pstrResult = new BLTriangulus().InserirICMSBaseReduzida(modSQL.ConnectionStringTriangulusCancelamento, objNF, Uteis.LogonName());
                if (pstrResult == 0)
                {
                    string mensagem = "O ICMS ST da nota " + objNF.NFNumero.ToString() + " serie " + objNF.Serie + " foi ajustado ";
                    return Json(new { result = "ok", data = mensagem });
                }
                else
                    return Json(new { result = "Error", data = "Não foi possível alterar o status " });
            }
            catch (Exception ex)
            {
                return Json(new { result = "Error", data = ex.Message });
            }
        }


        //Marcio Spinosa - 05/06/2019 - CR00009165 - Fim
    }
}
