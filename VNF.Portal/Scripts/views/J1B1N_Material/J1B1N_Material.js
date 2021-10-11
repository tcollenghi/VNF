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
        url: url + "J1B1N_Material/GetDataJ1B1N_Material",
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
    $("#txtIdItem").val('');
    $("#txtCodMaterial").val('');
    $("#hdfCodMaterial").val('');
    $("#txtCodMaterial").attr('disabled', false);
}

function Atualizar() {
    window.location = url + 'J1B1N_Material/Index';
}

function SalvarCodMaterial() {
    txtIdItem = $("#hdfIdItem").val();
    txtCodMaterial = $("#txtCodMaterial").val();
    pCodMaterial = $("#hdfCodMaterial").val();

    var erros = "";

    if (txtCodMaterial == "" || txtCodMaterial == 0) {
        erros += "<p>Informar o Código do Material</p>"
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
        UrlParametro = url + "J1B1N_Material/EditaJ1B1N_Material";
        pData = {
            pID: txtIdItem,
            pValor: txtCodMaterial
        }
    }    
    else {
        UrlParametro = url + "J1B1N_Material/SalvarJ1B1N_Material";
        pData = {
            ptxtValor: txtCodMaterial
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
            $("#txtIdItem").val('');
            $("#txtCodMaterial").val('');
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
    
    pIdItem = $("#hdfIdItem").val();
    pCodMaterial = $("#hdfCodMaterial").val();
    $("#txtIdItem").attr('disabled',true);
    $("#txtIdItem").val(pIdItem);
    $("#txtCodMaterial").val(pCodMaterial);
    $("#idItem").val(pEditar);
}

function Excluir() {
    pIdItem = $("#hdfIdItem").val();

    $.SmartMessageBox({
        title: "Excluir Material",
        content: "Deseja realmente excluir o código do material?",
        buttons: '[Não][Sim]'
    }, function (ButtonPressed) {
        if (ButtonPressed === "Sim") {
            $.ajax({
                async: false,
                cache: false,
                type: 'GET',
                url: url + "J1B1N_Material/ExcluirJ1B1N_Material",
                dataType: 'json',
                data: {
                    pID: pIdItem
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
        }
    });
    Atualizar();//Marcio Spinosa - 07/05/2018 - CR00008351

}