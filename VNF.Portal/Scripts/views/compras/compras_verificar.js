var Index = 0;
var PageSize = 30;
var motivoNaoEnvioSAP = "";

function selectRow(id, TipoDocumento) {
    $("#row" + id).siblings().removeClass("selecionado");
    $("#row" + id).toggleClass("selecionado");

    $("#hdfTipoDocumento").val(TipoDocumento);
    $("#hdfId").val(id);
    //alert(id);

    $("#btnRegistroFiscal").removeClass("disabled");
    $("#btnDetalhes").removeClass("disabled");

    $("#btnDanfeGrid").removeClass("disabled");

    $("#btnMaterialRecebido").removeClass("disabled");
    $("#btnEstornarMaterialRecebido").removeClass("disabled");

}


function ExcluirdAnexo(CodLog) {

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Ocorrencias/ExcluirAnexoOcorrencia",
        dataType: 'text',
        data: {
            id: CodLog
        },
        error: function () {
            $.smallBox({
                title: "VNF NELES",
                content: "Ocorreu um erro ao tentar excluir o anexo.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 2000
            });

        },
        success: function () {
            CarregaAnexos();
            $.smallBox({
                title: "VNF NELES",
                content: "Anexo excluido com sucesso.",
                color: "#739E73",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 2000
            });
        }
    });
}

function fn_IntegracaoSAPbtn(SITUACAO, VNF_TIPO_DOCUMENTO) {
    /// <summary>
    /// Esta Funcao desativa os botoes qdo for registros integrados do PORTAL DE SERVIÇO
    /// </summary>
    /// <param name="SITUACAO" type="type"></param>
    /// <param name="VNF_TIPO_DOCUMENTO" type="type"></param>
    if (SITUACAO == 'ACEITA' && (VNF_TIPO_DOCUMENTO == "NFS" || VNF_TIPO_DOCUMENTO == "FAT" || VNF_TIPO_DOCUMENTO == "TLC")) {
        $("#btnEnviarSAP").prop("disabled", true);
        $("#listBtn").prop("disabled", true);
        $("#btnCriarOcorrencia").prop("disabled", true);
        $("#btnRecusar").attr("disabled", "disabled");
    }
    else if (VNF_TIPO_DOCUMENTO == "NFS" || VNF_TIPO_DOCUMENTO == "FAT" || VNF_TIPO_DOCUMENTO == "TLC") {

        $("#btnEnviarSAP").prop("disabled", false);
        $("#listBtn").prop("disabled", false);
        $("#btnCriarOcorrencia").prop("disabled", false);
        $("#btnRecusar").attr("disabled", "disabled");
    }
    $("#HdnTipoDocumento").val(VNF_TIPO_DOCUMENTO);
}

function GetDanfe() {

    if ($("#hdfTipoDocumento").val() == "NFS" || $("#hdfTipoDocumento").val() == "FAT" || $("#hdfTipoDocumento").val() == "TLC") {
        var GetDanteUrl = url + 'Compras/DownloadAnexoPS?id=' + $('#hdfId').val();
        window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
    }
    else {
        if ($("#hdfTipoDocumento").val() == "TAL") {
            var GetDanteUrl = url + 'Talonario/DownloadAnexoChave?id=' + $('#hdfId').val();
            window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
        } else {

            var GetDanteUrl = url + 'Compras/DownloadDanfe?pIdNfe=' + $('#hdfId').val();
            window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
        }
    }
}

function readCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}


$(window).scroll(function () {
    if ($(window).scrollTop() >= 70) {
        $('.navigation-wrap').addClass('fixed');
    }
    else {
        $('.navigation-wrap').removeClass('fixed');
    }
    if ($(window).scrollTop() >= 108) {
        $('.dt-toolbar').addClass('toolfixed');
    }
    else {
        $('.dt-toolbar').removeClass('toolfixed');
    }
    if ($(window).scrollTop() >= 150) {
        $('.dataTables_scrollHeadInner').addClass('theadfixed');
    }
    else {
        $('.dataTables_scrollHeadInner').removeClass('theadfixed');
    }
});


$(".toggle input").click(function () {
    $(this).toggleClass("manual-mode");
});

$(document).ready(function () {
    if ($("#hdPermissaoAcesso").val() == 'false') return;

    pageSetUp();
    CarregaMotivos();

    setMaskCnpj("txtFornecedor");

    var filtros = readCookie('LoadDataVNFE');
    var filtro_unidades;
    if (filtros != null) {
        ShowLoading("Aguarde", "procurando documentos...");
        var aux = filtros.split('|');

        $("#txtNfeid").val(aux[0]); //Marcio Spinosa - 28/08/2018 - CRXXXXX
        $("#txtNumeroNf").val(aux[1]);
        $("#TipoNotaFiscal").val(aux[2]);//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
        $("#txtPasta").val(aux[3]);
        $("#UnidadeMetso").val(aux[4]);
        filtro_unidades = aux[4];
        $("#txtFornecedor").val(aux[5]);
        $("#Situacao").val(aux[6]);
        $("#TipoDocumento").val(aux[7]);
        $("#TipoFrete").val(aux[8]);
        $("#Relevante").val(aux[9]);
        $("#MaterialRecebido").val(aux[10]);
        $("#StatusIntegracao").val(aux[11]);
        $("#TipoData").val(aux[12]);
        $("#txtDataDe").val(aux[13]);
        $("#txtDataAte").val(aux[14]);
        $("#txtCodFornecedor").val(aux[15]);
        //   alert(filtros);
        $("#txtQtdRegistros").val(aux[16]);

        $(".exceedQtdMsg").val("");
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Compras/LoadDataVNFE",
            dataType: 'html',
            data: {
                pNfeid: aux[0],//Marcio Spinosa - 28/08/2018 - CRXXXXX
                pNumeroNf: aux[1],
                pTipoNotaFiscal: aux[2],//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
                pPasta: aux[3],
                pUnidade: aux[4],
                pFornecedor: aux[5],
                pSituacao: aux[6],
                pTipoDocumento: aux[7],
                pTipoFrete: aux[8],
                pRelevante: aux[9],
                pMaterialRecebido: aux[10],
                pStatusIntegracao: aux[11],
                pTipoData: aux[12],
                pDataDe: aux[13],
                pDataAte: aux[14],
                pCodFornecedor: aux[15],
                pQtdRegistros: aux[16]
            },
            error: function (erro) {
                $.SmartMessageBox({
                    title: "Ocorreu um erro inesperado no sistema.",
                    content: erro,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            },
            success: function (oReturn) {
                HideLoading();
                $('#divGrid').html(oReturn);
            }
        });
    }
    LoadUnidadesMetso(filtro_unidades);
    LoadSituacoesNf();
    LoadTiposData();
    LoadTiposFrete();
    LoadStatusIntegracao();
})

function LoadUnidadesMetso(pStrfiltro_unidades) {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Compras/GetUnidadesMetso",
        dataType: 'json',
        error: function () {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar a caixa de seleção de Unidade Metso!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.each(oReturn.UnidadesMetso, function (index, item) {
                if (pStrfiltro_unidades != null) {
                    if (pStrfiltro_unidades == (item.Value)) {
                        $('#UnidadeMetso').append(

                            $('<option selected="selected"></option>').val(item.Value).html(item.Text));
                    }
                    else {
                        $('#UnidadeMetso').append(

                            $('<option></option>').val(item.Value).html(item.Text)
                        );
                    }
                }
                else {
                    $('#UnidadeMetso').append(

                        $('<option></option>').val(item.Value).html(item.Text)
                    );
                }

            });
        }
    });
}

function LoadSituacoesNf() {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Compras/GetSituacoesNf",
        dataType: 'json',
        error: function () {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar a caixa de seleção de Situação NF!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.each(oReturn.SituacoesNf, function (index, item) {
                $('#Situacao').append(
                    $('<option></option>').val(item.Value).html(item.Text)
                );
            });
        }
    });
}

function LoadTiposData() {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Compras/GetTiposData",
        dataType: 'json',
        error: function () {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar a caixa de seleção de Data!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.each(oReturn.TiposData, function (index, item) {
                $('#TipoData').append(
                    $('<option></option>').val(item.Value).html(item.Text)
                );
            });
        }
    });
}

function LoadTiposFrete() {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Compras/GetTiposFrete",
        dataType: 'json',
        error: function () {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar a caixa de seleção de Tipo Frete!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.each(oReturn.TiposFrete, function (index, item) {
                $('#TipoFrete').append(
                    $('<option></option>').val(item.Value).html(item.Text)
                );
            });
        }
    });
}

function LoadStatusIntegracao() {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Compras/GetStatusIntegracao",
        dataType: 'json',
        error: function () {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar a caixa de seleção de Status Integracao!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.each(oReturn.StatusIntegracao, function (index, item) {
                $('#StatusIntegracao').append(
                    $('<option></option>').val(item.Value).html(item.Text)
                );
            });
        }
    });
}

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});


$(window).scroll(function () {
    if ($(window).scrollTop() + $(window).height() == $(document).height()) {
        // LoadData();
    }
});

$(document).keydown(function (e) {
    if (e.keyCode == 67 && e.ctrlKey) {
        if ($("#hdfId").val() != '' && $("#hdfId").val() !== undefined) {
            window.clipboardData.setData('Text', $("#hdfId").val());
            $.smallBox({
                title: "VNF NELES",
                content: "Chave do DOC-e copiado com sucesso.<br/><br/>",
                color: "#739E73",
                icon: "fa fa-file-text-o",
                timeout: 4000
            });
        }
    }
});

function RegistroFiscal() {
    ShowLoading("Aguarde", "carregando informações do documento");
    LoadDetalhe();
}

function Detalhes() {
    ShowLoading("Aguarde", "carregando detalhes do documento");
    var newUrl = url + '/Compras/Edit/' + $("#hdfId").val();
    window.location = newUrl.replace("//Compras", "/Compras");
}

function VisaoCompleta() {
    var newUrl = url + '/Compras/VerifVisaoCompleta/' + $('#hdfId').val();
    window.location = newUrl.replace("//Compras", "/Compras");
}

function FilterData() {
    $("#dttNotaFiscal tbody").empty();
    $("#dttNotaFiscal tfoot").show();
    $("#hdfId").val("");
    Index = 0;
    LoadData();
}

function LoadData() {
    ShowLoading("Aguarde", "procurando documentos...");
    var pNumeroNf = $("#txtNumeroNf").val();
    var pPasta = $("#txtPasta").val();
    var Unidade = $("#UnidadeMetso option:selected").val();
    var Fornecedor = $("#txtFornecedor").val();
    var Situacao = $("#Situacao option:selected").val();
    var TipoDocumento = $("#TipoDocumento option:selected").val();
    var TipoFrete = $("#TipoFrete option:selected").val();
    var Relevante = $("#Relevante option:selected").val();
    var MaterialRecebido = $("#MaterialRecebido option:selected").val();
    var StatusIntegracao = $("#StatusIntegracao option:selected").val();
    var TipoData = $("#TipoData option:selected").val();
    var DataDe = $("#txtDataDe").val();
    var DataAte = $("#txtDataAte").val();
    var CodFornecedor = $("#txtCodFornecedor").val();
    var QtdRegistros = $("#txtQtdRegistros").val();
    var pNfeid = $("#txtNfeid").val();//Marcio Spinosa - 28/08/2018 - CRXXXXX
    var pTipoNotaFiscal = $("#TipoNotaFiscal").val();//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165

    $(".exceedQtdMsg").val("");
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadDataVNFE",
        dataType: 'html',
        data: {
            pNfeid: pNfeid,//Marcio Spinosa - 28/08/2018 - CRXXXXX
            pNumeroNf: pNumeroNf,
            pTipoNotaFiscal: pTipoNotaFiscal,//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
            pPasta: pPasta,
            pUnidade: Unidade,
            pFornecedor: Fornecedor,
            pSituacao: Situacao,
            pTipoDocumento: TipoDocumento,
            pTipoFrete: TipoFrete,
            pRelevante: Relevante,
            pMaterialRecebido: MaterialRecebido,
            pStatusIntegracao: StatusIntegracao,
            pTipoData: TipoData,
            pDataDe: DataDe,
            pDataAte: DataAte,
            pCodFornecedor: CodFornecedor,
            pQtdRegistros: QtdRegistros
        },
        error: function (erro) {
            $.SmartMessageBox({
                title: "Ocorreu um erro inesperado no sistema.",
                content: erro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            HideLoading();
            $('#divGrid').html(oReturn);
            $("#demo-setting").trigger("click");
        }
    });
}

function ExportarNfe() {
    ShowLoading("Aguarde", "exportando documentos...");
    var pNumeroNf = $("#txtNumeroNf").val();
    var pPasta = $("#txtPasta").val();
    var Unidade = $("#UnidadeMetso option:selected").val();
    var Fornecedor = $("#txtFornecedor").val();
    var Situacao = $("#Situacao option:selected").val();
    var TipoDocumento = $("#TipoDocumento option:selected").val();
    var TipoFrete = $("#TipoFrete option:selected").val();
    var Relevante = $("#Relevante option:selected").val();
    var MaterialRecebido = $("#MaterialRecebido option:selected").val();
    var StatusIntegracao = $("#StatusIntegracao option:selected").val();
    var TipoData = $("#TipoData option:selected").val();
    var DataDe = $("#txtDataDe").val();
    var DataAte = $("#txtDataAte").val();
    var CodFornecedor = $("#txtCodFornecedor").val();
    var QtdRegistros = $("#txtQtdRegistros").val();
    var pNfeid = $("txtNFeid").val();//Marcio Spinosa - 28/08/2018 - CRXXXXX
    var pTipoNotaFiscal = $("#TipoNotaFiscal").val();//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/ExportVNFE",
        dataType: 'json',
        data: {
            pNfeid: pNfeid,//Marcio Spinosa - 28/08/2018 - CRXXXXX
            pNumeroNf: pNumeroNf,
            pTipoNotaFiscal: pTipoNotaFiscal,//Marcio Spinosa - 24/04/2019 - SR00265225 - CR00009165
            pPasta: pPasta,
            pUnidade: Unidade,
            pFornecedor: Fornecedor,
            pSituacao: Situacao,
            pTipoDocumento: TipoDocumento,
            pTipoFrete: TipoFrete,
            pRelevante: Relevante,
            pMaterialRecebido: MaterialRecebido,
            pStatusIntegracao: StatusIntegracao,
            pTipoData: TipoData,
            pDataDe: DataDe,
            pDataAte: DataAte,
            pCodFornecedor: CodFornecedor,
            pQtdRegistros: QtdRegistros
        },
        error: function (erro) {
            $.SmartMessageBox({
                title: "Ocorreu um erro inesperado no sistema.",
                content: erro,
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
            });
        },
        success: function (oReturn) {
            HideLoading();
            window.location = url + 'files/export/' + oReturn.fileName;
        }
    });
}


function EventoCodigoBarras() {

    pCodBarra = $("#txtCodBarras").val();

    if (event.keyCode == 13) {
        LoadData();
    }
}

function ShowNfeDetails(idRow) {
    $(".NotaFiscalDetails").addClass("collapse");

    if ($("#btnNfDetails" + idRow).hasClass("fa-chevron-up")) {
        $("#btnNfDetails" + idRow).addClass("fa-chevron-down");
        $("#btnNfDetails" + idRow).removeClass("fa-chevron-up");
    } else {
        $(".NotaFiscal" + idRow).removeClass("collapse");
        $(".fa-chevron-up").addClass("fa-chevron-down");
        $(".fa-chevron-up").removeClass("fa-chevron-up");
        $("#btnNfDetails" + idRow).removeClass("fa-chevron-down");
        $("#btnNfDetails" + idRow).addClass("fa-chevron-up");
    }
}

function MaterialRecebido() {
    var IdNfe = $("#hdfId").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/MaterialRecebido",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function (erro) {
            alert(oReturn.result);
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle fadeInLeft animated",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            if (oReturn.result === "true") {
                LoadData();
                $.smallBox({
                    title: "VNF NELES",
                    content: "Material recebido",
                    color: "#739E73",
                    icon: "fa fa-check-circle",
                    buttons: '[Fechar]',
                    timeout: 2000
                });
            }
            else {
                $.smallBox({
                    title: "Erros",
                    content: "Não foi possível completar sua solicitação, verifique se a nota já não foi integrada, cancelada ou recusada.",
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle fadeInLeft animated",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            }



        }
    });

}

function EstornarMaterialRecebido() {
    var IdNfe = $("#hdfId").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/EstornarMaterialRecebido",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function (erro) {
            alert(oReturn.result);
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle fadeInLeft animated",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            if (oReturn.result === "true") {
                LoadData();
                $.smallBox({
                    title: "VNF NELES",
                    content: "Estornar Material recebido",
                    color: "#739E73",
                    icon: "fa fa-check-circle",
                    buttons: '[Fechar]',
                    timeout: 2000
                });
            }
            else {
                $.smallBox({
                    title: "Erros",
                    content: "Não foi possível completar sua solicitação, verifique se a nota já não foi integrada, cancelada ou recusada.",
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle fadeInLeft animated",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            }



        }
    });

}

function LoadDetalhe() {
    hdfId = $("#hdfId").val();

    var cUrl = url;
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: cUrl + "Compras/LoadDataInfoVNFE",
        dataType: 'json',
        data: {
            pDocE: hdfId
        },
        error: function () {
            HideLoading();
        },
        success: function (oReturn) {
            HideLoading();
            motivoNaoEnvioSAP = "";

            //Verifica se é uma nota manual
            if (oReturn.IsNotaManualJ1B1N) {
                ShowLoading("Aguarde", "carregando dados para emissão da nota manual.");
                var _url = cUrl + "NotaManual_J1B1N/NotaManual?pNFEID=" + hdfId + "&pStrNFType=" + oReturn.NFType;
                window.location.href = _url;
            }
            else {
                $("#divDetalhe").modal();
                $("#divTabelaItens").empty();
                $("#divTabelaItens").append(oReturn.data);

                $("#btnDesfazer").addClass("hide");
                $("#btnRecusar").addClass("hide");
                $("#btnConfirmarDesfazer").addClass("hide");
                $("#btnConfirmarRecusa").addClass("hide");

                $("#textObservacao").val("");
                $("#lblTituloRecusa").empty();
                $("#hfTipoMiro").val(oReturn.TipoMiro);
                $("#divLog").empty();
                if (oReturn.Log == "") {
                    $("#divLog").append("<span>não existe informação</span>");
                } else {
                    $("#divLog").append(oReturn.Log);
                }

                $("#divCabecalho").empty();
                $("#divCabecalho").addClass("hide");
                if (oReturn.cabecalho != "") {
                    $("#divCabecalho").removeClass("hide");
                    $("#divCabecalho").empty();
                    $("#divCabecalho").append(oReturn.cabecalho);
                    $("#divSelecionarTipoMiro").hide();
                }

                if (oReturn.TipoMiro == "A") {
                    $("#chkMiroAutomatica").attr("checked", "checked");
                    $("#chkMiroAutomatica").removeAttr("disabled");
                    $("#chkMiroAutomatica").removeClass("manual-mode");
                    $("#lblMiroAutomatica").removeClass("state-disabled");
                } else {
                    $("#chkMiroAutomatica").removeAttr("checked");
                    $("#chkMiroAutomatica").attr("disabled", "disabled");
                    $("#chkMiroAutomatica").addClass("manual-mode");
                    $("#lblMiroAutomatica").addClass("state-disabled");
                }

                if (oReturn.recusada == "S") {
                    $("#btnDesfazer").removeClass("hide");
                    $("#btnEnviarIP").attr("disabled", "disabled");
                    $("#btnEnviarSAP").attr("disabled", "disabled");
                    $("#btnConfirmarDesfazer").removeClass("hide");
                    $("#lblTituloRecusa").append("<h4 class='modal-title'>Desfazer Recusa</h4>");
                }
                else {
                    $("#btnRecusar").removeClass("hide");
                    $("#btnConfirmarRecusa").removeClass("hide");
                    $("#lblTituloRecusa").append("<h4 class='modal-title'>Recusa no Verso</h4>");

                    if (oReturn.EnviadaIP == "Sim" || oReturn.StatusNF != "ACEITA") {
                        $("#btnEnviarIP").attr("disabled", "disabled");
                    } else {
                        $("#btnEnviarIP").removeAttr("disabled");
                    }
                    $("#btnEnviarIP").removeAttr("disabled");

                    if (oReturn.StatusNF != "ACEITA" || oReturn.EnviarSap == false) {
                        if (oReturn.StatusIntegracao == "CONCLUÍDO") {
                            motivoNaoEnvioSAP = "Integração já foi concluída.";
                        }
                        else if (oReturn.StatusNF != "ACEITA") {
                            motivoNaoEnvioSAP = "Nota Fiscal está com o status " + oReturn.StatusNF;
                        }
                        else if (oReturn.EnviarSap == false) {
                            motivoNaoEnvioSAP = "Não foi determinado o modo/processo dos itens.";
                        }
                    } else {
                        motivoNaoEnvioSAP = oReturn.MotivoNaoEnvioSap;
                    }
                }

                if (oReturn.PodeModificar == false) {
                    //Bloqueando controles 
                    //$("#btnDesfazer").attr("disabled", "disabled");
                    $("#btnEnviarIP").attr("disabled", "disabled");
                    $("#btnEnviarSAP").attr("disabled", "disabled");
                    $("#listBtn").attr("disabled", "disabled");
                    $("#btnConfirmarDesfazer").attr("disabled", "disabled");
                    $("#btnRecusar").attr("disabled", "disabled");
                    $("#btnConfirmarRecusa").attr("disabled", "disabled");
                    $("#btnCriarOcorrencia").attr("disabled", "disabled");
                    $("#btnUpload").attr("disabled", "disabled");
                    $("#FileInputfileAnexo").attr("disabled", "disabled");
                    $("#divDropDown").children().attr('disabled', 'disabled');

                    if (motivoNaoEnvioSAP == "") {
                        $.smallBox({
                            title: "VNF NELES",
                            content: "Documento exibido em mode de visualização devido a status de integração SAP já realizada ou nota com situação de recusa ou cancelada!<br/><br/>",
                            color: "#739E73",
                            icon: "fa fa-file-text-o",
                            timeout: 5000
                        });
                    }
                    else {
                        $.smallBox({
                            title: "VNF NELES",
                            content: "Documento exibido em mode de visualização devido ao seguinte(s) motivo(s): " + motivoNaoEnvioSAP + ".<br/><br/>",
                            color: "#739E73",
                            icon: "fa fa-file-text-o",
                            timeout: 5000
                        });
                    }
                }
                else {
                    //Desbloqueando controles 
                    //$("#btnDesfazer").removeAttr("disabled");
                    $("#btnEnviarIP").removeAttr("disabled");
                    $("#btnEnviarSAP").removeAttr("disabled");
                    $("#listBtn").removeAttr("disabled");
                    $("#btnConfirmarDesfazer").removeAttr("disabled");
                    $("#btnRecusar").removeAttr("disabled");
                    $("#btnConfirmarRecusa").removeAttr("disabled");
                    $("#btnCriarOcorrencia").removeAttr("disabled");
                    $("#btnUpload").removeAttr("disabled");
                    $("#FileInputfileAnexo").removeAttr("disabled");
                    $("#divDropDown").children().removeAttr("disabled");
                }

                //Verifica se o botão de estornar estará ativo. Ele só pode estar ativo caso a Integração com o SAP esteja concluída
                if (oReturn.IntegracaoConcluida == false) {
                    $("#btnEstornar").attr("disabled", "disabled");
                }
                else {
                    $("#btnEstornar").removeAttr("disabled");
                }

                var tipodoc = $("#HdnTipoDocumento").val();

                if (tipodoc == "NFS" || tipodoc == "FAT" || tipodoc == "TLC") {
                    $("#btnRecusar").attr("disabled", "disabled");
                }

                //Verifica se o modelo de documento deve ou não habilidar a opção de impressão de SLIP
                if (oReturn.ImprimirSLIP == false) {
                    $("#SLIPOption").addClass("hide");
                    $("#hfImprimeSLIP").val("false");
                }
                else {
                    $("#SLIPOption").removeClass("hide");
                    $("#chkImprimeSLIP").attr("checked", "checked");
                    $("#hfImprimeSLIP").val("true");
                }
                CarregaAnexos();
            }
        }
    });
}

function AlterarRateio(idRow) {

    var Nfeid = $("#trRow" + idRow).attr("nfeid");
    var IsRatear = $("#chkNf" + idRow).val() == "True";
    IsRatear = !IsRatear;

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/AlterarRateio",
        dataType: 'json',
        data: {
            pIdNfe: Nfeid,
            ratear: IsRatear
        },
        error: function () {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            if (oReturn.data == "") {
                $("#divDetalhe").modal("hide");
                ShowLoading("Aguarde", "recalculando distribuição do rateio");
                LoadDetalhe();
            } else {
                $.smallBox({
                    title: "Alterar rateio",
                    content: oReturn.data,
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            }
        }
    });

    CarregaAnexos();
}

function AlterarTipoMiro() {
    if ($("#hfTipoMiro").val() == "M") {
        $("#hfTipoMiro").val("A");
    } else {
        $("#hfTipoMiro").val("M");
    }
}

function onlyIntLocal(evt) {
    evt = (evt) ? evt : window.event
    if (evt.keyCode == 13) {
        FilterData();
    }
    var charCode = (evt.which) ? evt.which : evt.keyCode
    if (charCode > 31 && (charCode < 48 || charCode > 57)) {
        return false
    }
    return true
}

function MostraPainelOcorrencia() {

}

function CarregaMotivos() {
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetMotivoOcorrencia",
        dataType: 'html',
        error: function () {
        },
        success: function (oReturn) {
            $("#cboMotivoDivergenciaOC").empty();
            $("#cboMotivoDivergenciaOC").append(oReturn);
        }
    });
}

function MostraResponsaveis() {
    var id = $("#cboMotivoDivergenciaOC option:selected").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "MotivoCorrecao/SelecionaResponsavel",
        dataType: 'text',
        data: { id: id },
        error: function () {
        },
        success: function (oReturn) {
            if (oReturn == "S") {
                $("#ResponsavelContainer").removeClass("hide");
                CarregaResponsaveis();
            }
            else {
                $("#ResponsavelContainer").addClass("hide");
                $("#cboResponsavel").empty();
            }
        }
    });

}

function CarregaResponsaveis() {
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetResponsaveis",
        dataType: 'html',
        error: function () {
        },
        success: function (oReturn) {
            $("#cboResponsavel").empty();
            $("#cboResponsavel").append(oReturn);
        }
    });
}

function AbrirModalOcorrencia() {
    $("#divDetalhe").modal("hide");
    $("#divOcorrencia").modal();
}

function FecharModalOcorrencia() {
    $("#divOcorrencia").modal("hide");
    $("#divDetalhe").modal("show");
}
function CriarOcorrencia() {

    if ($("#hdfId").val() == "" || $("#txtCometario").val() == "") {
        $.SmartMessageBox({
            title: "Nova Ocorrência",
            content: "Preencha as informações obrigatorias",
            buttons: '[Ok]'
        });
    }
    else {
        var hdfId = $("#hdfId").val();
        var Motivo = $("#cboMotivoDivergenciaOC option:selected").val();
        var Comentario = $("#txtCometario").val();
        var Responsavel = $("#cboResponsavel option:selected").val();

        if (typeof FormData == "undefined") {
            var formdata = [];
        } else {
            var formdata = new FormData();
        }


        var fileInput = document.getElementById("FileAttachment");
        if (fileInput.files != null) {
            for (i = 0; i < fileInput.files.length; i++) {
                formdata.append(fileInput.files[i].name, fileInput.files[i]);
            }
        }

        formdata.append("NFEID", hdfId);
        formdata.append("Motivo", Motivo);
        formdata.append("Comentario", Comentario);
        formdata.append("Responsavel", Responsavel);

        $('html, body').animate({ scrollTop: 0 }, 'slow');
        ShowLoading("Aguarde", "Registrando ocorrência"); //Marcio Spinosa - 28/05/2018 - CR00008351
        $.ajax({
            url: url + "Compras/CriaOcorrencia",
            type: 'POST',
            data: formdata,
            processData: false,
            contentType: false,
            error: function () {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao registrar ocorrência, tente novamente. Caso o problema persista contate o suporte.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            },
            success: function () {
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Ocorrência criada com sucesso!",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });

            }

        });

        $("#cboMotivoDivergenciaOC").val("");
        $("#txtCometario").val("");
        $("#divOcorrencia").modal("hide");
        $("#divDetalhe").modal("show");

    }
}

function confirmarRecusa(tipo, observacao) {
    hdfId = $("#hdfId").val();
    //textObservacao = $("#textObservacao").val();

    //if (textObservacao == "") {
    //    $.SmartMessageBox({
    //        title: "Erros",
    //        content: "Informar a observação",
    //        buttons: '[Fechar]'
    //    })
    //    return false;
    //}

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/RecusaVNFE",
        dataType: 'json',
        data: {
            NfeId: hdfId,
            tipo: tipo,
            observacao: observacao
        },
        error: function () {
        },
        success: function (oReturn) {
            $('#btnFecharMotivo').trigger('click');
            LoadDetalhe();
            LoadData();
        }
    });
    return true;
}

function EnviarIP() {
    $("#btnEnviarIP").empty();
    $("#btnEnviarIP").append("Enviando...");
    $("#btnEnviarIP").addClass("disabled");
    hdfId = $("#hdfId").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/EnviarIP",
        dataType: 'json',
        data: {
            pIdNfe: hdfId
        },
        error: function () {
            $("#btnEnviarIP").empty();
            $("#btnEnviarIP").append("Enviar IP");
            $("#btnEnviarIP").removeClass("disabled");
        },
        success: function (oReturn) {
            $.smallBox({
                title: "VNF NELES",
                content: "Envio realizado com sucesso!",
                color: "#739E73",
                icon: "fa fa-file-text-o",
                timeout: 4000
            });
            $("#btnEnviarIP").empty();
            $("#btnEnviarIP").append("Enviar IP");
            $("#btnEnviarIP").removeClass("disabled");
        }
    });
}

function Recusar() {
    $("#divDetalhe").modal("hide");
    $("#MsgBoxBack").show();
    $.SmartMessageBox({
        title: "Informe o motivo da recusa",
        content: "",
        buttons: "[Confirmar][Fechar]",
        input: "text",
        placeholder: "",
        value: ""
    }, function (ButtonPress, Value) {
        if (ButtonPress == "Fechar") {
            HideLoading();
            $("#divDetalhe").modal("show");
            return 0;
        }
        if (ButtonPress == "Confirmar") {
            var Value1 = Value.toUpperCase();
            if (Value1 == "") {
                $.SmartMessageBox({
                    title: "Erros",
                    content: "Informar a observação",
                    buttons: '[Fechar]'
                }, function (ButtonPress, Value) {
                    if (ButtonPress == "Fechar") {
                        $("#divDetalhe").modal();
                    }
                });
            } else {
                confirmarRecusa("R", Value1);
                $("#divDetalhe").modal();
            }
        }
    });
};



function DesfazerRecusa() {
    $("#divDetalhe").modal("hide");
    $("#MsgBoxBack").show();
    $.SmartMessageBox({
        title: "Informe o motivo do cancelamento da recusa",
        content: "",
        buttons: "[Confirmar][Fechar]",
        input: "text",
        placeholder: "",
        value: ""
    }, function (ButtonPress, Value) {
        if (ButtonPress == "Fechar") {
            HideLoading();
            $("#divDetalhe").modal("show");
            return 0;
        }

        Value1 = Value.toUpperCase();
        if (Value1 == "") {
            $.SmartMessageBox({
                title: "Erro",
                content: "Informe a observação",
                buttons: '[Fechar]'
            });
        }
        else
            confirmarRecusa("D", Value1);

    });
};

function KeyEnter(e) {
    if (e.keyCode == 13) {
        FilterData();
    }
}

$("#btnEnviarSAP").click(function (e) {
    EnviarDocumento(e);
    e.preventDefault();
})

function EnviarDocumento(e) {
    $("#divDetalhe").modal("hide");
    $("#MsgBoxBack").show();

    if (motivoNaoEnvioSAP != "") {
        $.SmartMessageBox({
            title: "Não é possível enviar este documento",
            content: motivoNaoEnvioSAP,
            buttons: '[Ok]'
        }, function (ButtonPress, Value) {
            HideLoading();
            $("#divDetalhe").modal("show");
        });
    } else {
        var SapUserCookie = readCookie('UsuarioSAP_VNF');
        if (SapUserCookie == null || SapUserCookie == undefined) {
            SapUserCookie = "";
        }
        SapUserCookie = SapUserCookie.replace("\"", "").replace("\"", "");
        if (SapUserCookie == "null") {
            SapUserCookie = "";
        }

        var SapPasswordCookie = readCookie('SenhaSAP_VNF');
        if (SapPasswordCookie == null || SapPasswordCookie == undefined) {
            SapPasswordCookie = "";
        }
        SapPasswordCookie = SapPasswordCookie.replace("\"", "").replace("\"", "");
        if (SapPasswordCookie == "null") {
            SapPasswordCookie = "";
        }

        if (SapUserCookie != "") {
            $.SmartMessageBox({
                title: "Olá! <strong>" + SapUserCookie.toUpperCase() + ",</strong>",
                content: "Deseja enviar os dados para o SAP através deste usuário?",
                buttons: '[Sim][Não]'
            }, function (ButtonPress, Value) {
                if (ButtonPress == "Sim") {
                    EnviarSap(SapUserCookie, SapPasswordCookie);
                }
                if (ButtonPress == "Não") {
                    SolicitarUsuarioSap(e);
                }
            });
        } else {
            SolicitarUsuarioSap(e);
        }
    }
}

function RegistroManual() {
    $("#divDetalhe").modal("hide");
    $("#MsgBoxBack").show();

    var IdNfe = $("#hdfId").val();
    ShowLoading("Aguarde", "atualizando informações no sistema")
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/RegistroManual",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function () {
            $.SmartMessageBox({
                title: "Ocorreu uma falha no processamento",
                content: "repita novamente o processo ou envie este erro para o administrador",
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
                $("#divDetalhe").modal("show");
            });
        },
        success: function (oReturn) {
            if (oReturn.data == "") {
                $.SmartMessageBox({
                    title: "Registro manual",
                    content: "Os dados foram atualizados com sucesso.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    window.location.reload();
                });
            } else {
                $.SmartMessageBox({
                    title: "Não foi possível atualizar os dados",
                    content: oReturn.data,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    window.location.reload();
                });
            }
        }
    });
}

function SolicitarUsuarioSap(e) {
    HideLoading();
    $("#divSapLogon").show();
    e.preventDefault();
}

function CancelarDadosSAP() {
    $("#txtUsuarioSAP").val("");
    $("#txtSenhaSAP").val("");
    $("#divSapLogon").hide();
    $("#divDetalhe").modal("show");
}

function ConfirmarDadosSAP() {
    if ($("#txtUsuarioSAP").val() != "" && $("#txtSenhaSAP").val() != "") {
        $("#divSapLogon").hide();
        EnviarSap($("#txtUsuarioSAP").val(), $("#txtSenhaSAP").val());
    }
}

function EnviarSap(SapUser, SapPassword) {
    var IdNfe = $("#hdfId").val();
    var tipoMiro = $("#hfTipoMiro").val();
    var ImprimeSLIP = $("#hfImprimeSLIP").val();

    ShowLoading("Aguarde", "validando e enviando os dados desta nota fiscal para o SAP")
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/EnviarSap",
        dataType: 'json',
        data: {
            SapUser: SapUser,
            SapPassword: SapPassword,
            pIdNfe: IdNfe,
            pTipoMiro: tipoMiro,
            pStrImprimirSLIP: ImprimeSLIP
        },
        error: function () {
            $.SmartMessageBox({
                title: "Ocorreu uma falha no processamento",
                content: "repita novamente o processo ou envie este erro para o administrador",
                buttons: '[Ok]'
            }, function (ButtonPress, Value) {
                HideLoading();
                $("#divDetalhe").modal("show");
            });
        },
        success: function (oReturn) {
            if (oReturn.mensagem == "") {
                $.SmartMessageBox({
                    title: "Integração SAP",
                    content: "Os documentos foram processados com sucesso.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    window.location.reload();
                });
            } else {
                $.SmartMessageBox({
                    title: "Falha no processamento",
                    content: oReturn.mensagem,
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    window.location.reload();
                });
            }
        }
    });
}

function UploadAnexo() {
    hfIdNfe = $("#hdfId").val();

    if (typeof FormData == "undefined") {
        var formdata = [];
    } else {
        var formdata = new FormData();
    }


    var fileInput = document.getElementById("FileInputfileAnexo");
    if (fileInput.files != null) {
        for (i = 0; i < fileInput.files.length; i++) {
            formdata.append(fileInput.files[i].name, fileInput.files[i]);
        }
    }

    formdata.append("NFEID", hfIdNfe);
    var params;
    if (typeof FormData == "undefined") {
        params = JSON.stringify(formdata);
    } else {
        params = formdata;
    }

    $('html, body').animate({ scrollTop: 0 }, 'slow');

    var xhr = new XMLHttpRequest();
    xhr.open("POST", url + "Ocorrencias" + "/" + "UploadAnexo", true);
    xhr.setRequestHeader("Content-length", params.length);
    xhr.setRequestHeader("Connection", "close");
    xhr.send(params);

    xhr.onload = function () {
        window.scrollTo(0, 0);
        if (xhr.readyState == 4) {
            if (xhr.status == 200) {

                if (xhr.responseText != "") {
                    var postReturn = JSON.parse(xhr.responseText);
                }

                if (postReturn != undefined && postReturn.result == "Error") {
                    $.smallBox({
                        title: "Erros",
                        content: "Não foi possível completar sua solicitação!",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle",
                        buttons: '[Fechar]',
                        timeout: 4000
                    });
                } else {
                    CarregaAnexos();
                }
            }
            else {
                $.smallBox({
                    title: "Erros",
                    content: "Não foi possível completar sua solicitação!",
                    color: "#C46A69",
                    icon: "fa fa-exclamation-circle",
                    buttons: '[Fechar]',
                    timeout: 4000
                });
            }
        }
    }
}

function CarregaAnexos() {
    var NFEID = $('#hdfId').val();
    $("#lblQtdAnexos").text('');
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Ocorrencias/GetOcorrenciaAnexos",
        dataType: 'json',
        data: { NFEID: NFEID },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível completar sua solicitação!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $("#dtAnexos tbody").empty();
            $("#dtAnexos tbody").append(oReturn.html);
            if (oReturn.qtdOcorr > 0) {
                $("#lblQtdAnexos").append(oReturn.qtdOcorr);
                $("#lblQtdAnexos").show();
            } else {
                $("#lblQtdAnexos").hide();
            }
        }
    });
}

function Estornar() {
    $("#divDetalhe").modal("hide");
    $("#MsgBoxBack").show();
    $.SmartMessageBox({
        title: "Informe o motivo do estorno dos dados de integração no VNF",
        content: "",
        buttons: "[Confirmar][Fechar]",
        input: "text",
        placeholder: "",
        value: ""
    }, function (ButtonPress, Value) {
        if (ButtonPress == "Fechar") {
            HideLoading();
            $("#divDetalhe").modal("show");
            return 0;
        }
        if (ButtonPress == "Confirmar") {
            var Value1 = Value.toUpperCase();
            if (Value1 == "") {
                $.SmartMessageBox({
                    title: "Erros",
                    content: "Informar a observação",
                    buttons: '[Fechar]'
                }, function (ButtonPress, Value) {
                    if (ButtonPress == "Fechar") {
                        $("#divDetalhe").modal();
                    }
                });
            } else {
                EstornarVNF(Value1);
                $("#divDetalhe").modal();
            }
        }
    });
};


function EstornarVNF(observacao) {
    hdfId = $("#hdfId").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/EstornarVNF",
        dataType: 'json',
        data: {
            NfeId: hdfId,
            observacao: observacao
        },
        error: function () {
            $.SmartMessageBox({
                title: "Erros",
                content: "Erro ao estornar documento, tente novamente ou contate o suporte.",
                buttons: '[Fechar]'
            }, function (ButtonPress, Value) {
                if (ButtonPress == "Fechar") {
                    $("#divDetalhe").modal();
                }
            });
        },
        success: function (oReturn) {
            $('#btnFecharMotivo').trigger('click');
            LoadDetalhe();
            LoadData();
        }
    });
    return true;
}

function CheckUncheckSLIP() {
    if ($("#hfImprimeSLIP").val() == "true") {
        $("#hfImprimeSLIP").val("false");
    } else {
        $("#hfImprimeSLIP").val("true");
    }
}