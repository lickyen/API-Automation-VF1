pipeline {
    agent any

    environment {
        DOTNET_TOOLS = "$HOME/.dotnet/tools"
    }

    stages {

        stage('Checkout') {
            steps {
                git branch: 'main',
                    url: 'https://github.com/lickyen/API-Automation-VF1.git'
            }
        }

        stage('Verify Dotnet') {
            steps {
                sh 'dotnet --version'
            }
        }

        stage('Restore') {
            steps {
                sh 'dotnet restore'
            }
        }

        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release'
            }
        }

        stage('SonarQube Analysis') {
            steps {
                withSonarQubeEnv('SonarQube') {
                    sh """
                    export PATH=\$PATH:${DOTNET_TOOLS}

                    dotnet-sonarscanner begin \
                    /k:"API-Automation-VF1" \
                    /n:"API-Automation-VF1" \
                    /d:sonar.host.url=\$SONAR_HOST_URL \
                    /d:sonar.token=\$SONAR_AUTH_TOKEN

                    dotnet build --configuration Release

                    dotnet-sonarscanner end \
                    /d:sonar.token=\$SONAR_AUTH_TOKEN
                    """
                }
            }
        }

        stage('Quality Gate') {
            steps {
                timeout(time: 10, unit: 'MINUTES') {
                    waitForQualityGate abortPipeline: true
                }
            }
        }

        stage('Publish') {
            steps {
                sh 'dotnet publish --configuration Release -o publish'
            }
        }

        stage('Archive DLL') {
            steps {
                archiveArtifacts artifacts: 'publish/**/*.dll', fingerprint: true
            }
        }
    }

    post {
        success {
            echo 'Build Successful'
        }

        failure {
            echo 'Build Failed'
        }

        always {
            echo 'Pipeline Completed'
        }
    }
}
