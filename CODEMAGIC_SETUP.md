# BumperZ no Codemagic

O projeto contém dois workflows em `codemagic.yaml`:

- `bumperz-ios-testflight`: gera um IPA assinado e envia para o TestFlight.
- `bumperz-android-aab`: gera um Android App Bundle assinado.

## Configuração única no Codemagic

1. Adicione este repositório ao Codemagic e selecione configuração por `codemagic.yaml`.
2. Em **Team settings > Global variables and secrets**, crie o grupo secreto
   `unity_credentials` com `UNITY_EMAIL`, `UNITY_PASSWORD` e `UNITY_SERIAL`.
3. Em **Team integrations > Developer Portal**, adicione a chave da App Store
   Connect com o nome `codemagic` e permissão **App Manager**.
4. Em **Code signing identities**, gere ou envie um certificado Apple Distribution
   e um provisioning profile App Store para `com.bumperz.game`.
5. Crie o aplicativo `com.bumperz.game` no App Store Connect antes do primeiro envio.
6. Para Android, envie o keystore em **Android keystores** usando a referência
   `bumperz_keystore`. Guarde uma cópia segura fora do repositório.

Os certificados, perfis, chaves e senhas são externos e nunca devem ser commitados.

## Teste no iPhone

Execute `BumperZ iOS - TestFlight`. Quando o processamento da Apple terminar,
adicione sua conta ao grupo de testadores internos no TestFlight e instale o app.

## Requisitos de licença

O build Unity em CI usa `UNITY_SERIAL`; segundo a documentação atual do Codemagic,
é necessária uma licença Unity Plus ou Pro para ativação no servidor.
