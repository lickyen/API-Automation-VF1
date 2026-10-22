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
                bat 'dotnet --version'
            }
        }

        stage('Restore') {
            steps {
                bat 'dotnet restore'
            }
        }

        stage('Build') {
            steps {
                bat 'dotnet build --configuration Release'
            }
        }

        stage('Publish') {
            steps {
                bat 'dotnet publish --configuration Release -o publish'
            }
        }
    }
}
