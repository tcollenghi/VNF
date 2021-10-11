/// <reference path="../../libs/jquery-2.1.1.min.js" />

$(window).scroll(function () {
    if ($(window).scrollTop() + $(window).height() == $(document).height()) {
    }
});

function LoadData() {
    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: url + "J1B1N_Planta_Metso_Parceiro/GetData",
        dataType: 'json',
        data: {
        },
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Não foi possível concluir sua solicitação.",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {

            if (oReturn.data.length == 0) {

                $("#dttTabela tfoot").hide();
                ;
            } else {
                $("#dttTabela tbody").append(oReturn.data);
            }
        }
    });
}

function Novo() {
    $("#idItem").text("Novo");
    $("#txtCNPJ").val('');
    $("#txtPlanta").val('');
    $("#hdfPlanta").val('');
    $("#txtPlanta").attr('disabled', false);
}

function Atualizar() {
    window.location = url + 'J1B1N_Planta_Metso_Parceiro/Index';
}

function SalvarPadrao() {
    txtCNPJ = $("#txtCNPJ").val();
    txtPlanta = $("#txtPlanta").val();
    txtPlantaAntigo = $("#hdfPlanta").val();

    var erros = "";

    if (txtCNPJ == "" || txtCNPJ == 0) {
        erros += "<p>Informar o CNPJ</p>"
    }

    if (txtPlanta == "" || txtPlanta == 0) {
        erros += "<p>Informar a descrição o número da planta</p>"

    }

    if (erros != "") {
        $.smallBox({
            title: "Erros",
            content: erros,
            color: "#C46A69",
            icon: "fa fa-exclamation-circle",
            buttons: '[Fechar]',
            timeout: 1000
        });
        return;
    }

    var UrlParametro;
    var pData;

    var Editar = $("#idItem").val();
    if (Editar == 'EDITAR') {
        UrlParametro = url + "J1B1N_Planta_Metso_Parceiro/Editar";
        pData = {
            pCNPJ: txtCNPJ,
            pPlanta: txtPlanta
        }
    }
    else {
        UrlParametro = url + "J1B1N_Planta_Metso_Parceiro/Salvar";
        pData = {
            pCNPJ: txtCNPJ,
            pPlanta: txtPlanta
        }
    }

    //Atualizar();//Marcio Spinosa - 07/05/2018 - CR00008351

    $.ajax({
        async: false,
        cache: false,
        type: 'GET',
        url: UrlParametro,
        dataType: 'json',
        data: pData,
        error: function (erro) {
            $.smallBox({
                title: "Erros",
                content: "Erro ao efetuar o processo",
                color: "#C46A69",
                icon: "fa fa-exclamation-circle",
                buttons: '[Fechar]',
                timeout: 1000
            });
        },
        success: function (oReturn) {
            $("#txtCNPJ").val('');
            $("#txtPlanta").val('');
            $("#btnSalvarParametro").removeClass("disabled");
            $("#btnFecharModal").trigger('click');

            $.smallBox({
                title: "Alerta",
                content: "Processo realizada com sucesso",
                timeout: 1000,
                color: "#739E73",
                icon: "fa fa-check-circle",
            });
            Atualizar();
        }
    });
}

function Editar(pEditar) {
    $("#idItem").text("Editar");

    pCNPJ = $("#hdfCNPJ").val();
    pPlanta = $("#hdfPlanta").val();
    $("#txtIdItem").attr('disabled', true);
    $("#txtCNPJ").val(pCNPJ);
    $("#txtPlanta").val(pPlanta);
    
    $("#idItem").val(pEditar);
}

function Excluir() {
    pCNPJ = $("#hdfCNPJ").val();

    $.SmartMessageBox({
        title: "Excluir Parceiro",
        content: "Deseja realmente excluir o Parceiro?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: false,
                cache: false,
                type: 'GET',
                url: url + "J1B1N_Planta_Metso_Parceiro/Excluir",
                dataType: 'json',
                data: {
                    pCNPJ: pCNPJ
                },
                error: function (erro) {
                    $.smallBox({
                        title: "Erros",
                        content: "Não foi possível completar sua solicitação!",
                        color: "#C46A69",
                        icon: "fa fa-exclamation-circle fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 1000
                    });
                },
                success: function (oReturn) {
                    $.smallBox({
                        title: "VNF NELES",
                        content: "Exclusão efetuado com sucesso.",
                        color: "#739E73",
                        icon: "fa fa-trash-o fadeInLeft animated",
                        buttons: '[Fechar]',
                        timeout: 1000
                    });
                }
            });
            Atualizar();
        }
    });
    

}