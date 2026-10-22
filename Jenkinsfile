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
