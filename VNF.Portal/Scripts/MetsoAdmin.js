
//-----------------------------------------------------------------------------------------//
//----------------------------------> STANDARD VARIABLE <----------------------------------//
//-----------------------------------------------------------------------------------------//
var postReturn = null;
var postReturnValue = null;
var pathArray = window.location.href.split('/');
var protocol = pathArray[0];
var host = pathArray[2];
var app = pathArray[3];
var url = '';
if (host.indexOf("localhost") > -1) {
    url = protocol + '//' + host + '/';
} else {
    url = protocol + '//' + host + '/' + app + '/';
}


//----------------------------------------------------------------------------------//
//----------------------------------> INDEX PAGE <----------------------------------//
//----------------------------------------------------------------------------------//

function SelectRow(controller, id) {
    //$(".info").removeClass("info");
    //$(".disabled").removeClass("disabled");
    //$("#" + $.trim(id)).addClass("info");
    $("#hfId" + controller).val($.trim(id));
}

function Edit(controller) {
    window.location = url + controller + '/Edit/' + $("#hfId" + controller).val();
}




function validarData(data) {
    var dia = data.substring(0, 2);
    var mes = data.substring(3, 5);
    var ano = data.substring(6, 10);

    if (ano < 1900 || ano > 2050) {
        return false;
    } else if (mes > 12) {
        return false;
    } else if (mes == 1 || mes == 3 || mes == 5 || mes == 7 || mes == 8 || mes == 10 || mes == 12) {
        if (dia > 31) {
            return false
        }
    } else if (mes == 4 || mes == 6 || mes == 9 || mes == 11) {
        if (dia > 30) {
            return false
        }
    } else if (mes == 2) {
        if (dia > 29) {
            return false
        }
    };

    if (data.length > 10) {
        var hora = data.substring(11, 13);
        var minuto = data.substring(14, 16);

        if (hora > 24) {
            return false;
        } else if (minuto > 59) {
            return false;
        }
    };

    return true;
}

function validarCPF(cpf) {
    cpf = cpf.replace(/[^\d]+/g, '');

    if (cpf == '') {
        return false;
    }
    // Elimina CPFs invalidos conhecidos
    if (cpf.length != 11) {
        return false;
    }
    if (cpf == "00000000000" || cpf == "11111111111" ||
        cpf == "22222222222" || cpf == "33333333333" ||
        cpf == "44444444444" || cpf == "55555555555" ||
        cpf == "66666666666" || cpf == "77777777777" ||
        cpf == "88888888888" || cpf == "99999999999") {
        return false;
    }
    // Valida 1o digito 
    add = 0;
    for (i = 0; i < 9; i++) {
        add += parseInt(cpf.charAt(i)) * (10 - i); rev = 11 - (add % 11);
    }
    if (rev == 10 || rev == 11) {
        rev = 0;
    }
    if (rev != parseInt(cpf.charAt(9))) {
        return false;
    }

    // Valida 2o digito 
    add = 0;
    for (i = 0; i < 10; i++) {
        add += parseInt(cpf.charAt(i)) * (11 - i); rev = 11 - (add % 11);
    }
    if (rev == 10 || rev == 11) {
        rev = 0;
    }
    if (rev != parseInt(cpf.charAt(10))) {
        return false;
    }
    return true;
}

function validarCNPJ(cnpj) {

    cnpj = cnpj.replace(/[^\d]+/g, '');

    if (cnpj == '') { return false; }

    if (cnpj.length != 14) {
        return false;
    }

    // Elimina CNPJs invalidos conhecidos
    if (cnpj == "00000000000000" || cnpj == "11111111111111" ||
        cnpj == "22222222222222" || cnpj == "33333333333333" ||
        cnpj == "44444444444444" || cnpj == "55555555555555" ||
        cnpj == "66666666666666" || cnpj == "77777777777777" ||
        cnpj == "88888888888888" || cnpj == "99999999999999") {
        return false;
    }

    // Valida DVs
    tamanho = cnpj.length - 2
    numeros = cnpj.substring(0, tamanho);
    digitos = cnpj.substring(tamanho);
    soma = 0;
    pos = tamanho - 7;
    for (i = tamanho; i >= 1; i--) {
        soma += numeros.charAt(tamanho - i) * pos--;
        if (pos < 2) {
            pos = 9;
        }
    }
    resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
    if (resultado != digitos.charAt(0)) {
        return false;
    }

    tamanho = tamanho + 1;
    numeros = cnpj.substring(0, tamanho);
    soma = 0;
    pos = tamanho - 7;
    for (i = tamanho; i >= 1; i--) {
        soma += numeros.charAt(tamanho - i) * pos--;
        if (pos < 2) {
            pos = 9;
        }
    }
    resultado = soma % 11 < 2 ? 0 : 11 - soma % 11;
    if (resultado != digitos.charAt(1)) {
        return false;
    }

    return true;
}


jQuery(function ($) {
    $(".mask-cnpj").mask("99.999.999/9999-99");
    $(".mask-cep").mask("99.999-999");
    $(".mask-phone").mask("(99) 9999-9999");
    $(".mask-cellphone").mask("(99) 99999-9999");
    $(".mask-data").mask("99/99/9999");
    //$(".mask-number").maskMoney({ prefix: "", thousands: ".", decimal: ",", affixesStay: false, allowZero: false, precision: 0 });
    //$(".mask-float").maskMoney({ prefix: "", thousands: ".", decimal: ",", affixesStay: false, allowZero: true, precision: 2 });
    //$(".mask-floatprecision").maskMoney({ prefix: "", thousands: ".", decimal: ",", affixesStay: false, allowZero: true, precision: 5 });
    //$(".mask-currencyreal").maskMoney({ prefix: 'R$ ', allowNegative: true, thousands: '.', decimal: ',', affixesStay: true });
    //$(".mask-currencydolar").maskMoney({ prefix: "US$ ", thousands: ",", decimal: ".", affixesStay: true, allowZero: true });
    //$(".mask-currencyeuro").maskMoney({ prefix: "€ ", thousands: ",", decimal: ".", affixesStay: true, allowZero: true });
});


function setHfDropDownValue(dropDown) {
    var hfName = dropDown.id.replace("ddl", "");
    $("#" + hfName).val($("#" + dropDown.id + " option:selected").val());
}

