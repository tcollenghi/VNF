var Index = 0;
var PageSize = 30;
var responsiveHelper_dt_basic1 = undefined;

var breakpointDefinition = {
    tablet: 1024,
    phone: 480
};

$(document).ready(function () {
    pageSetUp();
    setMaskCnpj("RecebedorCNPJ");
    setMaskCnpj("FornecedorCNPJ");
    $("#btnConfirmarResposta").removeClass("disabled");

    var Status = "";
    var CookiePage = $("#hdCookiePage").val();

    var filtros = readCookie('ConsultaOcorrencia' + CookiePage);
    if (filtros != null) {
        ShowLoading("Aguarde", "procurando documentos...");
        var aux = filtros.split('|');

        $("#RecebedorCNPJ").val(aux[0]);
        $("#DataEnvioDe").val(aux[1]);
        $("#DataEnvioAte").val(aux[2]);
        $("#VencimentoDe").val(aux[3]);
        $("#VencimentoAte").val(aux[4]);
        $("#FornecedorCNPJ").val(aux[5]);
        $("#NumeroDocumento").val(aux[6]);
        Status = aux[7];
        if (Status == "") Status = $("#hdStatus").val();
        if (Status == "") Status = "Retornado";
        $('#Status').val(Status);
        var Responsavel = $("#hdResponsavel").val();

        $(".exceedQtdMsg").val("");
        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Ocorrencias/LoadData",
            dataType: 'html',
            data: {
                RecebedorCNPJ: aux[0],
                DataEnvioDe: aux[1],
                DataEnvioAte: aux[2],
                VencimentoDe: aux[3],
                VencimentoAte: aux[4],
                FornecedorCNPJ: aux[5],
                NumeroDocumento: aux[6],
                Status: Status,
                Responsavel: Responsavel,
                CookiePage: CookiePage
            },
            error: function (erro) {
                ExistMsg = 0;
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

    LoadStatus(Status);
    LoadResponsavel();

    if (filtros == null) {
        FilterData();
    }
})

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

function LoadStatus(Status) {
    var statusPadrao = $("#hdStatus").val();
    if (Status != "") statusPadrao = Status;

    $.ajax({
        async: false,
        cache: false,
        type: 'POST',
        url: url + "Ocorrencias/GetStatus",
        dataType: 'json',
        data: {
            statusPadrao: statusPadrao,
        },
        error: function () {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar a caixa de seleção de Status!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.each(oReturn.Status, function (index, item) {
                var opt = $('<option></option>').val(item.Value).html(item.Text);
                if (item.Selected) opt.prop("selected", "selected");
                $('#Status').append(opt);
            });
        }
    });
}

function LoadResponsavel() {
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Ocorrencias/GetResponsaveis",
        dataType: 'json',
        error: function () {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível carregar a caixa de seleção de Responsavel!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            $.each(oReturn.Responsaveis, function (index, item) {
                $('#cboEncaminhar').append(
                     $('<option></option>').val(item.Value).html(item.Text)
                 );
            });
        }
    });
}

function FilterData() {
    $("#dttTabela tbody").empty();
    $("#dttTabela tfoot").show();
    $("#hdfId").val("");

    Index = 0;
    LoadData();
}

function LoadData() {
    ShowLoading("Aguarde", "procurando documentos...");

    var RecebedorCNPJ = $("#RecebedorCNPJ").val();
    var DataEnvioDe = $("#DataEnvioDe").val();
    var DataEnvioAte = $("#DataEnvioAte").val();
    var VencimentoDe = $("#VencimentoDe").val();
    var VencimentoAte = $("#VencimentoAte").val();
    var FornecedorCNPJ = $("#FornecedorCNPJ").val();
    var NumeroDocumento = $("#NumeroDocumento").val();
    var Status = $("#Status option:selected").val();
    var Responsavel = "";//$("#hdResponsavel").val();
    var CookiePage = $("#hdCookiePage").val();

    $(".exceedQtdMsg").val("");
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Ocorrencias/LoadData",
        dataType: 'html',
        data: {
            RecebedorCNPJ: RecebedorCNPJ,
            DataEnvioDe: DataEnvioDe,
            DataEnvioAte: DataEnvioAte,
            VencimentoDe: VencimentoDe,
            VencimentoAte: VencimentoAte,
            FornecedorCNPJ: FornecedorCNPJ,
            NumeroDocumento: NumeroDocumento,
            Status: Status,
            Responsavel: Responsavel,
            CookiePage: CookiePage
        },
        error: function (erro) {
            ExistMsg = 0;
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

//$('#dttTabela').dataTable({
//    "sDom": "<'dt-toolbar'<'col-xs-12 col-sm-6'f><'col-sm-6 col-xs-12 hidden-xs'l>r>" +
//        "t" +
//        "<'dt-toolbar-footer'<'col-sm-6 col-xs-12 hidden-xs'i><'col-xs-12 col-sm-6'p>>",
//    "autoWidth": false,
//    "preDrawCallback": function () {
//        // Initialize the responsive datatables helper once.
//        if (!responsiveHelper_dt_basic1) {
//            responsiveHelper_dt_basic1 = new ResponsiveDatatablesHelper($('#dttTabela'), breakpointDefinition);
//        }
//    },
//    "rowCallback": function (nRow) {
//        responsiveHelper_dt_basic1.createExpandIcon(nRow);
//    },
//    "drawCallback": function (oSettings) {
//        responsiveHelper_dt_basic1.respond();
//    }
//});

function selectRow(id, NFEID, VNF_TIPO_DOCUMENTO) {
    $("#row_" + id).siblings().removeClass("selecionado");
    $("#row_" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
    $("#NFEID").val(NFEID);
    $("#VNF_TIPO_DOCUMENTO").val(VNF_TIPO_DOCUMENTO);

    //VErifica o status
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Ocorrencias/GetOCSTatus",
        dataType: 'text',
        data: { id: id },
        success: function (oReturn) {
            if (oReturn != "Finalizado") {
                $("#btnDanfe").removeClass("hide");
                //$("#btnResponder").removeClass("hide");
                $("#btnEncaminharOC").removeClass("hide");
                $("#btnFinalizarOc").removeClass("hide");
                $("#btnUpload").removeClass("hide");
            }
            else {
                $("#btnDanfe").addClass("hide");
                //$("#btnResponder").addClass("hide");
                $("#btnEncaminharOC").addClass("hide");
                $("#btnFinalizarOc").addClass("hide");
                $("#btnUpload").addClass("hide");
            }

            if (oReturn != "Pendente") {
                $("#btnResponder").addClass("hide");
            } else {
                $("#btnResponder").removeClass("hide");
            }
        }
    });
}


function UploadAnexo() {
    hfIdNfe = $("#NFEID").val();

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

function GetDanfe() {
    if ($("#VNF_TIPO_DOCUMENTO").val() == "NFS" || $("#VNF_TIPO_DOCUMENTO").val() == "FAT" || $("#VNF_TIPO_DOCUMENTO").val() == "TLC") {
        var GetDanteUrl = url + 'Compras/DownloadAnexoPS?id=' + $('#VNF_CHAVE_ACESSO').val();
        window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
    }
    else {
        if ($("#VNF_TIPO_DOCUMENTO").val() == "TAL") {
            var GetDanteUrl = url + 'Talonario/DownloadAnexoChave?id=' + $('#NFEID').val();
            window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
        } else {
            var GetDanteUrl = url + 'Compras/DownloadDanfe?pIdNfe=' + $('#NFEID').val();
            window.open(GetDanteUrl, "DANFE", "toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=yes, resizable=yes, width=780, height=500");
        }
    }
}

function Finalizar() {
    ExistMsg = 0;
    $.SmartMessageBox({
        title: "Confirmação",
        content: "Deseja finalizar a ocorrência?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            var IdOcorrencia = $("#hdfId").val();
            $.ajax({
                async: true,
                cache: false,
                type: 'POST',
                url: url + "Ocorrencias/Finalizar",
                dataType: 'text',
                data: { IdOcorrencia: IdOcorrencia },
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
                    if (oReturn != "ok") {
                        $.smallBox({
                            title: "Erros",
                            content: oReturn,
                            color: "#C46A69",
                            icon: "fa fa-exclamation-circle",
                            buttons: '[Fechar]',
                            timeout: 4000
                        });
                    }
                    else {
                        location.reload();
                    }
                }
            });
        }
        HideLoading();
    });
}

function ConfirmarResposta() {
    var IdOcorrencia = $("#hdfId").val();
    var Texto = $("#txtResposta").val();
    if (Texto == "") {
        $.smallBox({
            title: "Informação",
            content: "Preencha o texto da resposta.",
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        });
    }
    else {
        ShowLoading("Aguarde", "Respondendo ocorrência");//Marcio Spinosa - 28/05/2018 - CR00008351
        $.ajax({
            async: true,
            cache: false,
            type: 'POST',
            url: url + "Ocorrencias/Responder",
            dataType: 'text',
            data: { IdOcorrencia: IdOcorrencia, Texto: Texto },
            error: function () {
                ExistMsg = 0;
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao responder ocorrência, tente novamente. Caso o problema persista contate o suporte.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                });
            },
            success: function (oReturn) {
                if (oReturn != "ok") {
                    ExistMsg = 0;
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Erro ao responder ocorrência, tente novamente. Caso o problema persista contate o suporte.",
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                    });
                }
                else {
                    $("#btnConfirmarResposta").addClass("disabled");
                    ExistMsg = 0;
                    $.SmartMessageBox({
                        title: "VNF NELES",
                        content: "Ocorrência encaminhada com sucesso!",
                        buttons: '[Ok]'
                    }, function (ButtonPress, Value) {
                        HideLoading();
                        location.reload();
                    });
                   
                }
            }
        });
    }
}

function Encaminhar() {
    var IdOcorrencia = $("#hdfId").val();
    var Responsavel = $("#cboEncaminhar").val();
    var Comentario = $("#txtEncaminhar").val();

    if (Responsavel == "") {
        $.smallBox({
            title: "Informação",
            content: "Preencha o nome do responsável.",
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 4000
        });
    }
    else {
        ShowLoading("Aguarde", "Encaminhando ocorrência");//Marcio Spinosa - 28/05/2018 - CR00008351
        $.ajax({
            async: true,
            cache: false,
            type: 'POST',
            url: url + "Ocorrencias/Encaminhar",
            dataType: 'text',
            data: {
                IdOcorrencia: IdOcorrencia,
                Responsavel: Responsavel,
                Comentario: Comentario
            },
            error: function () {
                ExistMsg = 0;
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Erro ao encaminar ocorrência, tente novamente. Caso o problema persista contate o suporte.",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                    location.reload();
                });
            },
            success: function () {
                ExistMsg = 0;
                $.SmartMessageBox({
                    title: "VNF NELES",
                    content: "Ocorrência encaminhada com sucesso!",
                    buttons: '[Ok]'
                }, function (ButtonPress, Value) {
                    HideLoading();
                    $("#divOcorrencia").addClass("hide");
                    location.reload();
                });
            }
        });
        $("#cboEncaminhar").val("");
        $("#txtEncaminhar").val("");
        $("#divEncaminhar").modal("hide");
    }
}

function GetOcorrencia() {
    var IdOcorrencia = $("#hdfId").val();
    $("#txtMotivo").val("");
    $("#DataRecebimento").val("");
    $("#txtDataEsperada").val("");

    // limpa histórico
    $("#dtHistorico tbody").empty();
    $("#dtHistorico tbody").append("<tr><td colspan='4'>Carregando ...</td></tr>");

    // limpa anexos
    $("#dtAnexos tbody").empty();
    $("#dtAnexos tbody").append("<tr><td colspan='3'>Carregando ...</td></tr>");

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetOcorrencia",
        dataType: 'json',
        data: { IdOcorrencia: IdOcorrencia },
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
            $("#txtMotivo").val(oReturn.MotivoCorrecao);
            $("#DataRecebimento").val(oReturn.DataRecebimento);
            $("#txtDataEsperada").val(oReturn.DataEsperada);
            if (oReturn.Usuario == oReturn.Responsavel) {
                //$("#btnResponder").removeClass("hide");
                //$("#btnEncaminharOC").removeClass("hide");
            }
            else {
                //$("#btnResponder").addClass("hide");
                //$("#btnEncaminharOC").addClass("hide");
            }

            //Verifica se é do Fiscal e permite finalizar a ocorrência
            if (oReturn.FinalizaOcorrencia == "S") {
                $("#btnFinalizarOc").removeClass("hide");
            }
            else {
                $("#btnFinalizarOc").addClass("hide");
            }

            if (oReturn.AlteraQualquerOcorrencia !== "S" && oReturn.Status !== "Pendente") {
                $("#btnResponder").addClass("hide");
                $("#btnEncaminharOC").addClass("hide");
            }
            CarregaHistorico();
            CarregaAnexos();
        }
    });
}

function CarregaHistorico() {
    var IdOcorrencia = $("#hdfId").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Compras/GetOcorrenciaHistorico",
        dataType: 'text',
        data: { IdOcorrencia: IdOcorrencia },
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
            $("#dtHistorico tbody").empty();
            $("#dtHistorico tbody").append(oReturn);
        }
    });
}

//function MostrarHistorico() {
//    CarregaHistorico();
//    $("#tabHistorico").removeClass("hide");
//    $("#tabOcorrencia").addClass("hide");
//    $("#tabAnexos").addClass("hide");
//}

function KeyEnter(e) {
    if (e.keyCode == 13) {
        FilterData();
    }
}

//function MostrarOcorrencia() {
//    $("#tabOcorrencia").removeClass("hide");

//    $("#tabHistorico").addClass("hide");
//    $("#tabAnexos").addClass("hide");
//}

//function MostrarAnexos() {
//    CarregaAnexos();
//    $("#tabAnexos").removeClass("hide");
//    $("#tabOcorrencia").addClass("hide");
//    $("#tabHistorico").addClass("hide");
//}

function CarregaAnexos() {
    var NFEID = $('#NFEID').val();
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

function DownloadAnexo(CodLog) {
    window.location = url + "Compras/DownloadAnexo?CodLog=" + CodLog;
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

$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});


function Exportar() {
    $("#divPostingPage").removeClass("hide");
    $("#divPostingPage").show();
    $('html, body').animate({ scrollTop: 0 }, 'slow');

    RecebedorCNPJ = $("#RecebedorCNPJ").val();
    DataEnvioDe = $("#DataEnvioDe").val();
    DataEnvioAte = $("#DataEnvioAte").val();
    VencimentoDe = $("#VencimentoDe").val();
    VencimentoAte = $("#VencimentoAte").val();
    FornecedorCNPJ = $("#FornecedorCNPJ").val();
    NumeroDocumento = $("#NumeroDocumento").val();
    Status = $("#Status").val();


    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Ocorrencias/Exportar",
        dataType: 'html',
        data: {
            RecebedorCNPJ: RecebedorCNPJ,
            DataEnvioDe: DataEnvioDe,
            DataEnvioAte: DataEnvioAte,
            VencimentoDe: VencimentoDe,
            VencimentoAte: VencimentoAte,
            FornecedorCNPJ: FornecedorCNPJ,
            NumeroDocumento: NumeroDocumento,
            Status: Status
        },
        error: function () {
            $("#divPostingPage").addClass("hide");
        },
        success: function (oReturn) {
            $("#divPostingPage").addClass("hide");
            window.location = url + 'files/export/' + oReturn;
        }
    });
}
