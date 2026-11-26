pipeline {
    agent any

    environment {
        DOCKER_USER = 'nakul465'
        DOTNET_ROOT = '/usr/share/dotnet'
        KUBECONFIG = '/var/lib/jenkins/.kube/config'
        PATH = "/usr/local/bin:/usr/bin:/bin:/usr/share/dotnet:${env.PATH}"
    }

    stages {
        stage('Checkout') {
            steps {
                echo 'Checking out code from Git...'
                checkout scm
            }
        }

        stage('Build .NET Solution') {
            steps {
                echo 'Building .NET Solution...'
                sh 'dotnet build -c Release'
            }
        }

        stage('Build Docker Images') {
            steps {
                echo 'Building Docker Images for all 4 microservices...'
                sh '''
                    docker build --build-arg PROJECT_PATH="Fundoo.ApiGateway/Fundoo.ApiGateway.csproj" -t $DOCKER_USER/fundoo-gateway:live .
                    docker build --build-arg PROJECT_PATH="Fundoo.User.Api/Fundoo.User.Api.csproj" -t $DOCKER_USER/fundoo-user:live .
                    docker build --build-arg PROJECT_PATH="Fundoo.Notes.Api/Fundoo.Notes.Api.csproj" -t $DOCKER_USER/fundoo-notes:live .
                    docker build --build-arg PROJECT_PATH="Fundoo.Notification.Api/Fundoo.Notification.Api.csproj" -t $DOCKER_USER/fundoo-notification:live .
                '''
            }
        }

        stage('Push to Docker Hub') {
            steps {
                echo 'Pushing Docker Images to Docker Hub...'
                withCredentials([usernamePassword(credentialsId: 'docker-hub-credentials', usernameVariable: 'DOCKER_HUB_USER', passwordVariable: 'DOCKER_HUB_PASS')]) {
                    sh '''
                        echo "$DOCKER_HUB_PASS" | docker login -u "$DOCKER_HUB_USER" --password-stdin
                        docker push $DOCKER_USER/fundoo-gateway:live
                        docker push $DOCKER_USER/fundoo-user:live
                        docker push $DOCKER_USER/fundoo-notes:live
                        docker push $DOCKER_USER/fundoo-notification:live
                        docker logout
                    '''
                }
            }
        }

        stage('Deploy to K3s') {
            steps {
                echo 'Applying Kubernetes manifests and restarting deployments...'
                sh '''
                    kubectl apply -f k8s/
                    kubectl rollout restart deployment gateway-deploy user-deploy notes-deploy notification-deploy
                    kubectl rollout status deployment gateway-deploy --timeout=120s
                    kubectl rollout status deployment user-deploy --timeout=120s
                    kubectl rollout status deployment notification-deploy --timeout=120s
                    kubectl rollout status deployment notes-deploy --timeout=120s
                '''
            }
        }
    }

    post {
        success {
            echo '========================================================='
            echo '🎉 Fundoo CI/CD Pipeline Built and Deployed Successfully!'
            echo '========================================================='
        }
        failure {
            echo '❌ Pipeline execution failed. Inspect stage logs above.'
        }
    }
}
