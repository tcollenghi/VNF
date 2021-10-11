var Index = 0;
var PageSize = 30;
var arrItensNCM = [];
var contador = 0;
var Email = "";
var IdForencedor = "";

function RegimeEspecial() {
    if ($("#hdfId").val() != "") {
        $("#btnRegimeEspecial").attr("data-toggle", "modal");
        $("#btnRegimeEspecial").attr("data-target", "#modalNovaPasta");

        contador = 0;
        arrItensNCM = [];
        $("#tblItens tbody").empty();
        $("#txtItens").val("");

    }
    else {
        $.SmartMessageBox({
            title: "Erros",
            content: "Selecione uma nota",
            buttons: '[Fechar]'
        });
    }
}
function selectRow(id, re) {
    $("#row_" + id).siblings().removeClass("selecionado");
    $("#row_" + id).toggleClass("selecionado");
    $("#hdfId").val(id);
    $("#hdfRE").val(re);
    $("#txtEmail").val($("#tdEmail_row_" + id).text());
    IdForencedor = $("#tdIdFornecedor_row_" + id).text();
}

function UpdateEmail() {
 
    var Email = $("#txtEmail").val();
    $.ajax({
        async: true,
        cache: false,
        type: 'POST',
        url: url + "Fornecedores/UpdateEmail",
        dataType: 'html',
        data: {
            Id: IdForencedor,
            Email: Email 
        },
        error: function (erro) {
            alert(erro.me);
        },
        success: function (oReturn) {
            $('#btnFecharEmail').trigger("click");
            var timer = window.setTimeout(function () {
                LoadData();
                $("#demo-setting").trigger("click");
                window.clearTimeout(timer);
            }, 2000);
        }
    });
}

function ViewDados(controller) {
    if ($("#hdfId").val() != "") {
        window.location = url + controller + '/ViewDados/' + $("#hdfId").val();
    }
    else {
        $.SmartMessageBox({
            title: "Erros",
            content: "Selecione uma nota",
            buttons: '[Fechar]'
        });
    }
}




$(document).ready(function () {
    setMaskCnpj("txtCNPJ");
})



$("#demo-setting").on('click', function () {
    $(".demo").toggleClass("activate");
});


function LoadData() {

    $("#demo-setting").trigger("click");
    if ($("#txtCNPJ").val() == "" && $("#txtRazaoSocial").val() == "" && $("#txtCodigoSAP").val()=="") {
        $.SmartMessageBox({
            title: "Alerta",
            content: "Preencha os campos para realizar a busca.",
            buttons: '[Fechar]'
        });
        return;
    }

    $("#divDataNotas").addClass("hide");
    $("#divDataFornecedores").removeClass("hide");

    txtCNPJ = $("#txtCNPJ").val();
    txtRazaoSocial = $("#txtRazaoSocial").val();
    txtCodigoSAP = $("#txtCodigoSAP").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Fornecedores/GetData",
        dataType: 'html',
        data: {
            pStart: Index,
            pLength: PageSize,
            pCNPJ: txtCNPJ,
            pRazaoSocial: txtRazaoSocial,
            pCodigoSAP: txtCodigoSAP
        },
        error: function (erro) {
        },
        success: function (oReturn) {
            $('#divGrid').html(oReturn);
        }
    });
}

function NotasFiscais() {
    $("#divDataFornecedores").addClass("hide");
    $("#divDataNotas").removeClass("hide");

    if ($("#txtCNPJ").val() != "") {


        txtCNPJ = $("#txtCNPJ").val();


        $.ajax({
            async: true,
            cache: false,
            type: 'GET',
            url: url + "Fornecedores/NotasFiscais",
            dataType: 'json',
            data: {
                pStart: Index,
                pLength: PageSize,
                pCNPJ: txtCNPJ

            },
            error: function (erro) {
            },
            success: function (oReturn) {
                $("#dttTabelaNotas tbody").empty();
                $("#dttTabelaNotas tfoot").show();
                if (oReturn.data.length == 0) {

                    if (Index == 0)
                        $("#dttTabelaNotas tbody").append("<tr><td colspan='6' class='center'>não existe informação</td></tr>");

                    $("#dttTabelaNotas tfoot").hide();
                    Index = undefined;
                } else {
                    $("#dttTabelaNotas tbody").append(oReturn.data);

                    if (oReturn.pagina >= PageSize) {
                        Index++;
                    } else {
                        Index = undefined;
                        $("#dttTabelaNotas tfoot").hide();
                    }
                }


            }
        });

    }
    else {
        alert('Selecione um fornecedor')
    }
}

function Exportar() {
    txtCNPJ = $("#txtCNPJ").val();
    txtRazaoSocial = $("#txtRazaoSocial").val();
    txtCodigoSAP = $("#txtCodigoSAP").val();

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Fornecedores/Exportar",
        dataType: 'html',
        data: {
            pCNPJ: txtCNPJ,
            pRazaoSocial: txtRazaoSocial,
            pCodigoSAP: txtCodigoSAP

        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao tentar exportar!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            window.location = url + 'files/export/' + oReturn;
        }
    });
}


function AdicionarNCM() {
    arrItensNCM.push($("#txtItens").val());
    $("#tblItens tbody").empty();
    for (var i = 0; i < arrItensNCM.length; i++) {
        $("#tblItens tbody").append("<tr id='trItens" + arrItensNCM[i] + "'><td style='width:30%'><button type='button' class='fa  fa-times' onclick='RemoveNCM(&#39;" + arrItensNCM[i] + "&#39;)'><span class=''></span></button></td><td>" + arrItensNCM[i] + "</td></tr>");
    }
    $("#txtItens").val("");
    contador++;
}

    function RemoveNCM(id) {
        var index = 0;
        for (var i = 0; i < arrItensNCM.length; i++) {
            if (arrItensNCM[i] == id) {
                break;
            }
            index++;
        }

        if (index > -1) {

            $("#trItens" + arrItensNCM[index]).remove();

            arrItensNCM.splice(index, 1);
        }
        contador--;
    }


function SalvarNCM() {
    txtCNPJ = $("#hdfId").val();
    txtRE = $("#hdfRE").val();
    var itens = "";
    var virgula = "";

    for (i = 0 ; i < arrItensNCM.length; i++) {
        itens += virgula + arrItensNCM[i];
        virgula = ","
    }

    $.ajax({
        async: true,
        cache: false,
        type: 'GET',
        url: url + "Fornecedores/RegimeEspecial",
        dataType: 'json',
        data: {
            ptxtRE: txtRE,
            pCNPJ: txtCNPJ,
            pArrItens: itens

        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao atualizar o registro!",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 4000
            });
        },
        success: function (oReturn) {
            ExibirMensagem(oReturn.mensagem);
            $("#btnFecharNCM").trigger("click");
        }
    });

}

function ExibirMensagem(msg) {
    if (msg != "") {
        $.smallBox({
            title: "Alerta",
            content: msg,
            timeout: 4000
        });
    }
}