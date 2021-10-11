$(document).ready(function () {
    pageSetUp();
    LoadDivergencia();
    LoadDatasComparacoes();
    LoadMensagens();
    setMaskCnpj("cnpjDestinatario");
    setMaskCnpj("cnpj");
})

function DownloadAnexo(CodLog) {
    window.location = url + "Compras/DownloadAnexo?CodLog=" + CodLog;
}

function CarregaAnexos() {
    var NFEID = $('#VNF_CHAVE_ACESSO').val();
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

function UploadAnexo() {
    hfIdNfe = $("#VNF_CHAVE_ACESSO").val();

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

function Modificar() {
    if ($("#hfIdNfe").val() != "")
        window.location = url + 'Associacao/Edit/' + $("#hfIdNfe").val();
}

function LoadDatasComparacoes() {
    $("#dttDatasComparacoes tbody").empty();
    $("#dttDatasComparacoes tfoot").show();

    IdNfe = $("#hfIdNfe").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadDatasComparacoes",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function () {
        },
        success: function (oReturn) {
            if (oReturn.data.length == 0) {
                $("#dttDatasComparacoes tbody").append("<tr><td colspan='2' class='center'>não existe informação</td></tr>");
                $("#dttDatasComparacoes tfoot").hide();
                Index = undefined;
            } else {
                $("#dttDatasComparacoes tbody").append(oReturn.data);
                $("#dttDatasComparacoes tfoot").hide();
            }
        }
    });
}

function LoadComparacoes(pData) {

    var res = pData.split(" ");

    $("#row" + res[0].replace('/', '').replace('/', '') + res[1].replace(':','').replace(':','')).siblings().removeClass("selecionado");
    $("#row" + res[0].replace('/', '').replace('/', '') + res[1].replace(':', '').replace(':', '')).toggleClass("selecionado");

    $("#divComparacoes").empty();
    $("#divComparacoes").append("<span>carregando...</span>");
    
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadComparacoes",
        dataType: 'json',
        data: {
            pData: pData,
            pNfeID: $("#hfIdNfe").val()
        },
        error: function () {
        },
        success: function (oReturn) {
            $("#divComparacoes").empty();
            if (oReturn.data.length == 0) {
                $("#divComparacoes").append("<span>não existe informação</span>");
                Index = undefined;
            } else {
                $("#divComparacoes").append(oReturn.data);
            }
        }
    });
}

function LoadMensagens() {
    $("#divMensagens").empty();
    IdNfe = $("#hfIdNfe").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadDataMensagens",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function () {
        },
        success: function (oReturn) {
            carregouMensagem = true;
            if (oReturn.data.length == 0) {
                $("#divMensagens").append("não existe informação");
                Index = undefined;
            } else {
                $("#divMensagens").append(oReturn.data);
            }
        }
    });
}

function LoadDivergencia() {
    $("#divDivergencia").empty();
    IdNfe = $("#hfIdNfe").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/LoadDataDivergenciaNfe",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function () {
        },
        success: function (oReturn) {
            carregouMensagem = true;
            if (oReturn.data.length == 0) {
                $("#divDivergencia").append("não existe informação");
                $("#lblPendentes").hide();
                Index = undefined;
            } else {
                $("#divDivergencia").append(oReturn.data);

                $("#lblPendentes").append(oReturn.pendentes);
                if (oReturn.pendentes > 0) {
                    $("#lblPendentes").show();
                } else {
                    $("#lblPendentes").hide();
                }
            }
        }
    });
}

function ReenviarEmail() {
    ShowLoading("Aguarde", "Reenviando e-mail.");
    IdNfe = $("#hfIdNfe").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/ReenviarEmail",
        dataType: 'json',
        data: {
            pIdNfe: IdNfe
        },
        error: function () {
            HideLoading();
            $.smallBox({
                title: "VNF NELES",
                content: "Ocorreu um erro ao reenviar e-mail",
                color: "#C46A69",
                iconSmall: "fa fa-times fa-2x fadeInRight animated",
                timeout: 4000
            });
        },
        success: function (oReturn) {
            HideLoading();
            $.smallBox({
                title: "VNF NELES",
                content: "E-mail reenviado",
                color: "#739E73",
                icon: "fa fa-check-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
            window.location.reload(); 
        }
    });
}

function GetXml() {
    IdNfe = $("#hfIdNfe").val();
    var urlXml = url + "Compras/GetXml/" + IdNfe;
    window.open(urlXml, '_blank');
}

function GetDanfe() {
   
    if ($("#VNF_TIPO_DOCUMENTO").val() == "NFS" || $("#VNF_TIPO_DOCUMENTO").val() == "FAT" || $("#VNF_TIPO_DOCUMENTO").val() == "TLC") {
        var GetDanteUrl = url + 'Compras/DownloadAnexoPS?id=' + $('#VNF_CHAVE_ACESSO').val();
        window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
    }
    else {
        if ($("#VNF_TIPO_DOCUMENTO").val() == "TAL") {
            var GetDanteUrl = url + 'Talonario/DownloadAnexoChave?id=' + $('#VNF_CHAVE_ACESSO').val();
            window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
        } else {
            var GetDanteUrl = url + 'Compras/DownloadDanfe?pIdNfe=' + $('#VNF_CHAVE_ACESSO').val();
            window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
        }
    }
}

function DesfazerCancelamento() {
    var IdNfe = $("#hfIdNfe").val();

    $.SmartMessageBox({
        title: "Desfazer cancelamento",
        content: "Deseja realmente desfazer o cancelamento do documento?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: true,
                cache: false,
                type: 'GET',
                url: url + "Compras/DesfazerCancelamento",
                dataType: 'json',
                data: {
                    pIdNfe: IdNfe
                },
                error: function (erro) {
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
                    location.reload();
                }
            });
        }
    });
}

function MaterialRecebido() {
    var IdNfe = $("#hfIdNfe").val();
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
            window.location.reload(true);

            $("#btnEstornarMaterialRecebido").removeClass("disabled");
            $("#btnEstornarMaterialRecebido").removeClass("hide");

            $("#btnMaterialRecebido").addClass("disabled");
            $("#btnMaterialRecebido").addClass("hide");

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
    var IdNfe = $("#hfIdNfe").val();
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
            window.location.reload(true);
            $("#btnEstornarMaterialRecebido").addClass("disabled");
            $("#btnEstornarMaterialRecebido").addClass("hide");

            $("#btnMaterialRecebido").removeClass("disabled");
            $("#btnMaterialRecebido").removeClass("hide");

            if (oReturn.result == "true") {
                LoadData();
                $.smallBox({
                    title: "VNF NELES",
                    content: "Estornar material recebido",
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

function MostaDiv() {
    $("#tabDivergencias").removeClass("hide");

    $("#tabComparacoes").addClass("hide");
    $("#tabItens").addClass("hide");
    $("#tabMensagens").addClass("hide");
    $("#tabDocumentosRelacionados").addClass("hide");
    $("#tabLog").addClass("hide");
}
function MostaComp() {
    $("#tabComparacoes").removeClass("hide");

    $("#tabDivergencias").addClass("hide");
    $("#tabItens").addClass("hide");
    $("#tabMensagens").addClass("hide");
    $("#tabDocumentosRelacionados").addClass("hide");
    $("#tabLog").addClass("hide");
}
function MostaItens() {
    $("#tabItens").removeClass("hide");

    $("#tabComparacoes").addClass("hide");
    $("#tabDivergencias").addClass("hide");
    $("#tabMensagens").addClass("hide");
    $("#tabDocumentosRelacionados").addClass("hide");
    $("#tabLog").addClass("hide");
}
function MostaMsg() {
    $("#tabMensagens").removeClass("hide");

    $("#tabComparacoes").addClass("hide");
    $("#tabDivergencias").addClass("hide");
    $("#tabItens").addClass("hide");
    $("#tabDocumentosRelacionados").addClass("hide");
    $("#tabLog").addClass("hide");
}
function MostaDocsRelacionados() {
    $("#tabDocumentosRelacionados").removeClass("hide");

    $("#tabComparacoes").addClass("hide");
    $("#tabDivergencias").addClass("hide");
    $("#tabItens").addClass("hide");
    $("#tabMensagens").addClass("hide");
    $("#tabLog").addClass("hide");
}

function MostraLog() {
    $("#tabLog").removeClass("hide");

    $("#tabComparacoes").addClass("hide");
    $("#tabDivergencias").addClass("hide");
    $("#tabItens").addClass("hide");
    $("#tabMensagens").addClass("hide");
    $("#tabDocumentosRelacionados").addClass("hide");
}