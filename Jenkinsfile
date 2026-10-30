pipeline {
    agent any

    stages {

        stage('Checkout') {
            steps {
                git branch: 'main',
                    url: 'https://github.com/lickyen/API-Automation-VF1.git'
            }
        }

        stage('Verify Dotnet') {
            steps {
                sh '''
                export DOTNET_ROOT=/usr/lib64/dotnet
                export PATH=$PATH:/usr/lib64/dotnet

                dotnet --version
                '''
            }
        }

        stage('Restore') {
            steps {
                sh '''
                export DOTNET_ROOT=/usr/lib64/dotnet
                export PATH=$PATH:/usr/lib64/dotnet

                dotnet restore
                '''
            }
        }

        stage('Build') {
            steps {
                sh '''
                export DOTNET_ROOT=/usr/lib64/dotnet
                export PATH=$PATH:/usr/lib64/dotnet

                dotnet build --configuration Release
                '''
            }
        }

        stage('SonarQube Analysis') {
            steps {
                withSonarQubeEnv('SonarQube') {
                    sh '''
                    export DOTNET_ROOT=/usr/lib64/dotnet
                    export PATH=$PATH:/usr/lib64/dotnet:/opt/dotnet-tools

                    dotnet-sonarscanner begin \
                      /k:"Jenkins-VF-Pipeline-JOB" \
                      /n:"Jenkins-VF-Pipeline-JOB" \
                      /d:sonar.host.url="$SONAR_HOST_URL" \
                      /d:sonar.token="$SONAR_AUTH_TOKEN"

                    dotnet build --configuration Release

                    dotnet-sonarscanner end \
                      /d:sonar.token="$SONAR_AUTH_TOKEN"
                    '''
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
                sh '''
                export DOTNET_ROOT=/usr/lib64/dotnet
                export PATH=$PATH:/usr/lib64/dotnet

                dotnet publish --configuration Release -o publish
                '''
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
